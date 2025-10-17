using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MRP
{
    // DTO for creating/updating media
    public class MediaDto
    {
        // Accept string GUIDs from JSON and parse them explicitly
        public string? uuid { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? mediaType { get; set; }
        public int? releaseYear { get; set; }
        public string? ageRestriction { get; set; }
        public string? genre { get; set; }
        public string? createdBy { get; set; }
    }

    public sealed class MediaHTTPEndpoint : IHttpEndpoint
    {
        private readonly List<string> paths = new List<string> { "/api/media" };

        private readonly MediaRepository _mediaRepository;
        private readonly UserRepository _userRepository;
        private readonly RatingRepository _ratingRepository;

        public MediaHTTPEndpoint(MediaRepository mediaRepository, UserRepository userRepository, RatingRepository ratingRepository)
        {
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
        }

        public bool CanHandle(HttpListenerRequest request)
        {
            var path = request.Url!.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            return path.StartsWith(paths[0]);
        }

        public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
        {
            var req = context.Request;

            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                // support ?id=GUID, ?creator=GUID, ?title=...
                var idq = req.QueryString["id"];
                if (!string.IsNullOrWhiteSpace(idq) && Guid.TryParse(idq, out var id))
                {
                    var ent = _mediaRepository.GetMediaById(id);
                    if (ent == null) { await HttpServer.Json(context.Response, 404, new { error = "Media not found" }); return; }
                    await HttpServer.Json(context.Response, 200, ent);
                    return;
                }

                var creatorq = req.QueryString["creator"];
                if (!string.IsNullOrWhiteSpace(creatorq) && Guid.TryParse(creatorq, out var creatorId))
                {
                    var list = _mediaRepository.GetMediaByCreator(creatorId) ?? new List<MediaEntry>();
                    await HttpServer.Json(context.Response, 200, list);
                    return;
                }

                var titleq = req.QueryString["title"]; 
                if (!string.IsNullOrWhiteSpace(titleq))
                {
                    var list = _mediaRepository.GetMediaByTitle(titleq) ?? new List<MediaEntry>();
                    await HttpServer.Json(context.Response, 200, list);
                    return;
                }

                // return all
                await HttpServer.Json(context.Response, 200, _mediaRepository.GetAll());
                return;
            }

            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    var dto = JsonSerializer.Deserialize<MediaDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto == null || string.IsNullOrWhiteSpace(dto.title) || string.IsNullOrWhiteSpace(dto.createdBy))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing required fields: title, createdBy" });
                        return;
                    }

                    if (!Guid.TryParse(dto.createdBy, out var createdByGuid))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Invalid createdBy GUID" });
                        return;
                    }

                    var user = _userRepository.GetUserById(createdByGuid);
                    if (user == null) { await HttpServer.Json(context.Response, 400, new { error = "createdBy user not found" }); return; }

                    // parse enums
                    if (!Enum.TryParse<EMediaType>(dto.mediaType ?? "Movie", true, out var mtype)) mtype = EMediaType.Movie;
                    if (!Enum.TryParse<EFSK>(dto.ageRestriction ?? "FSK0", true, out var fsk)) fsk = EFSK.FSK0;

                    var entry = new MediaEntry(dto.title!, mtype, dto.releaseYear ?? DateTime.Now.Year, fsk, dto.genre ?? string.Empty, user, dto.description ?? string.Empty);
                    var newId = _mediaRepository.AddMedia(entry);
                    if (newId == Guid.Empty) { await HttpServer.Json(context.Response, 409, new { error = "Media already exists" }); return; }

                    await HttpServer.Json(context.Response, 201, new { message = "Media created", uuid = newId });
                }
                catch (JsonException ex)
                {
                    await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    await HttpServer.Json(context.Response, 500, new { error = $"Server error: {ex.Message}" });
                }

                return;
            }

            if (req.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    var dto = JsonSerializer.Deserialize<MediaDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto == null || string.IsNullOrWhiteSpace(dto.uuid) || !Guid.TryParse(dto.uuid, out var uuid))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid media uuid for update" });
                        return;
                    }

                    var existing = _mediaRepository.GetMediaById(uuid);
                    if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Media not found" }); return; }

                    // update fields
                    if (!string.IsNullOrWhiteSpace(dto.title)) existing.title = dto.title;
                    if (dto.description != null) existing.description = dto.description;
                    if (!string.IsNullOrWhiteSpace(dto.genre)) existing.genre = dto.genre;
                    if (dto.releaseYear != null) existing.releaseYear = dto.releaseYear.Value;
                    if (!string.IsNullOrWhiteSpace(dto.mediaType) && Enum.TryParse<EMediaType>(dto.mediaType, true, out var mtype)) existing.mediaType = mtype;
                    if (!string.IsNullOrWhiteSpace(dto.ageRestriction) && Enum.TryParse<EFSK>(dto.ageRestriction, true, out var fsk)) existing.ageRestriction = fsk;

                    // repository is in-memory list; replace item to persist change if necessary
                    var list = _mediaRepository.GetAll();
                    list.RemoveAll(m => m.uuid == existing.uuid);
                    list.Add(existing);

                    await HttpServer.Json(context.Response, 200, new { message = "Media updated" });
                }
                catch (JsonException ex)
                {
                    await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    await HttpServer.Json(context.Response, 500, new { error = $"Server error: {ex.Message}" });
                }

                return;
            }

            if (req.HttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                var idq = req.QueryString["id"];
                if (string.IsNullOrWhiteSpace(idq) || !Guid.TryParse(idq, out var id))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'id' query parameter" });
                    return;
                }

                var existing = _mediaRepository.GetMediaById(id);
                if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Media not found" }); return; }

                _mediaRepository.GetAll().Remove(existing);

                // remove ratings for this media
                var ratingsToRemove = _ratingRepository.GetAll().Where(r => r.mediaEntry == id).ToList();
                foreach (var r in ratingsToRemove) _ratingRepository.GetAll().Remove(r);

                await HttpServer.Json(context.Response, 200, new { message = "Media deleted" });
                return;
            }

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }
    }
}
