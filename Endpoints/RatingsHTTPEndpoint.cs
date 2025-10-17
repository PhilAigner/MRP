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
    // DTO for ratings create/update
    public class RatingDto
    {
        public string? uuid { get; set; }
        public string? mediaEntry { get; set; }
        public string? user { get; set; }
        public int? stars { get; set; }
        public string? comment { get; set; }
        public bool? publicVisible { get; set; }
    }

    public sealed class RatingsHTTPEndpoint : IHttpEndpoint
    {
        private readonly List<string> paths = new List<string> { "/api/ratings" };

        private readonly RatingRepository _ratingRepository;
        private readonly UserRepository _userRepository;
        private readonly MediaRepository _mediaRepository;
        private readonly ProfileRepository _profileRepository;
        private readonly MediaService _mediaService;

        public RatingsHTTPEndpoint(RatingRepository ratingRepository, UserRepository userRepository, MediaRepository mediaRepository, ProfileRepository profileRepository)
        {
            _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _mediaService = new MediaService(_mediaRepository, _ratingRepository, _profileRepository);
        }

        public bool CanHandle(HttpListenerRequest request)
        {
            var path = request.Url!.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            return path.StartsWith(paths[0]);
        }

        public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
        {
            var req = context.Request;

            // GET: /api/ratings?id=GUID | ?creator=GUID | ?media=GUID | ?minStars=N | ?maxStars=N
            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                var idq = req.QueryString["id"];
                if (!string.IsNullOrWhiteSpace(idq) && Guid.TryParse(idq, out var id))
                {
                    var r = _ratingRepository.GetById(id);
                    if (r == null) { await HttpServer.Json(context.Response, 404, new { error = "Rating not found" }); return; }
                    await HttpServer.Json(context.Response, 200, r);
                    return;
                }

                var creatorq = req.QueryString["creator"];
                if (!string.IsNullOrWhiteSpace(creatorq) && Guid.TryParse(creatorq, out var creatorId))
                {
                    var list = _ratingRepository.GetByCreator(creatorId) ?? new List<Rating>();
                    await HttpServer.Json(context.Response, 200, list);
                    return;
                }

                var mediaq = req.QueryString["media"];
                if (!string.IsNullOrWhiteSpace(mediaq) && Guid.TryParse(mediaq, out var mediaId))
                {
                    var list = _ratingRepository.GetAll().Where(r => r.mediaEntry == mediaId).ToList();
                    await HttpServer.Json(context.Response, 200, list);
                    return;
                }

                var minStarsQ = req.QueryString["minStars"];
                if (!string.IsNullOrWhiteSpace(minStarsQ) && int.TryParse(minStarsQ, out var minStars))
                {
                    var list = _ratingRepository.GetByStarsGreaterEqlThan(minStars) ?? new List<Rating>();
                    await HttpServer.Json(context.Response, 200, list);
                    return;
                }

                var maxStarsQ = req.QueryString["maxStars"];
                if (!string.IsNullOrWhiteSpace(maxStarsQ) && int.TryParse(maxStarsQ, out var maxStars))
                {
                    var list = _ratingRepository.GetByStarsLowerEqlThan(maxStars) ?? new List<Rating>();
                    await HttpServer.Json(context.Response, 200, list);
                    return;
                }

                // default: return all
                await HttpServer.Json(context.Response, 200, _ratingRepository.GetAll());
                return;
            }

            // POST: create rating
            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    var dto = JsonSerializer.Deserialize<RatingDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto == null || string.IsNullOrWhiteSpace(dto.mediaEntry) || string.IsNullOrWhiteSpace(dto.user) || dto.stars == null)
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing required fields: mediaEntry, user, stars" });
                        return;
                    }

                    if (!Guid.TryParse(dto.mediaEntry, out var mediaGuid) || !Guid.TryParse(dto.user, out var userGuid))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Invalid GUID for mediaEntry or user" });
                        return;
                    }

                    //Teste ob userId und MediaEntry existiert
                    var user = _userRepository.GetUserById(userGuid);
                    if (user == null)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "User not found" });
                        return;
                    }

                    var media = _mediaRepository.GetMediaById(mediaGuid);
                    if (media == null)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "Media entry not found" });
                        return;
                    }

                    // Use MediaService to track statistics
                    bool success;
                    if (!string.IsNullOrWhiteSpace(dto.comment))
                    {
                        success = _mediaService.rateMediaEntryWithComment(mediaGuid, userGuid, dto.stars.Value, dto.comment);
                    }
                    else
                    {
                        success = _mediaService.rateMediaEntry(mediaGuid, userGuid, dto.stars.Value);
                    }

                    if (!success)
                    {
                        await HttpServer.Json(context.Response, 500, new { error = "Failed to create rating" });
                        return;
                    }

                    // Get the created rating to return its ID
                    var createdRating = _ratingRepository.GetAll()
                        .FirstOrDefault(r => r.mediaEntry == mediaGuid && r.user == userGuid);
                    
                    if (createdRating != null && dto.publicVisible.HasValue)
                    {
                        createdRating.publicVisible = dto.publicVisible.Value;
                    }

                    await HttpServer.Json(context.Response, 201, new { message = "Rating created", uuid = createdRating?.uuid ?? Guid.Empty });
                    return;
                }
                catch (JsonException ex)
                {
                    await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON: {ex.Message}" });
                    return;
                }
                catch (Exception ex)
                {
                    await HttpServer.Json(context.Response, 500, new { error = $"Server error: {ex.Message}" });
                    return;
                }
            }

            // PUT: update rating by uuid
            if (req.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    var dto = JsonSerializer.Deserialize<RatingDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto == null || string.IsNullOrWhiteSpace(dto.uuid) || !Guid.TryParse(dto.uuid, out var uuid))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'uuid' for update" });
                        return;
                    }

                    var existing = _ratingRepository.GetById(uuid);
                    if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Rating not found" }); return; }

                    // Track comment changes for review statistics
                    bool hadComment = !string.IsNullOrWhiteSpace(existing.comment);
                    bool willHaveComment = dto.comment != null && !string.IsNullOrWhiteSpace(dto.comment);
                    
                    if (dto.stars != null) existing.stars = dto.stars.Value;
                    if (dto.comment != null) existing.comment = dto.comment;
                    if (dto.publicVisible != null) existing.publicVisible = dto.publicVisible.Value;

                    // Update review count if comment status changed
                    if (!hadComment && willHaveComment)
                    {
                        var profile = _profileRepository.GetByOwnerId(existing.user);
                        if (profile != null)
                        {
                            profile.numberOfReviewsWritten++;
                        }
                    }
                    else if (hadComment && !willHaveComment)
                    {
                        var profile = _profileRepository.GetByOwnerId(existing.user);
                        if (profile != null && profile.numberOfReviewsWritten > 0)
                        {
                            profile.numberOfReviewsWritten--;
                        }
                    }

                    // ensure repository list contains updated object (in-memory list)
                    var list = _ratingRepository.GetAll();
                    list.RemoveAll(r => r.uuid == existing.uuid);
                    list.Add(existing);

                    await HttpServer.Json(context.Response, 200, new { message = "Rating updated" });
                    return;
                }
                catch (JsonException ex)
                {
                    await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON: {ex.Message}" });
                    return;
                }
                catch (Exception ex)
                {
                    await HttpServer.Json(context.Response, 500, new { error = $"Server error: {ex.Message}" });
                    return;
                }
            }

            // DELETE: /api/ratings?id=GUID
            if (req.HttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                var idq = req.QueryString["id"];
                if (string.IsNullOrWhiteSpace(idq) || !Guid.TryParse(idq, out var id))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'id' query parameter" });
                    return;
                }

                var existing = _ratingRepository.GetById(id);
                if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Rating not found" }); return; }

                // Update profile statistics before deletion
                var profile = _profileRepository.GetByOwnerId(existing.user);
                if (profile != null)
                {
                    if (profile.numberOfRatingsGiven > 0)
                    {
                        profile.numberOfRatingsGiven--;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(existing.comment) && profile.numberOfReviewsWritten > 0)
                    {
                        profile.numberOfReviewsWritten--;
                    }
                }

                _ratingRepository.GetAll().RemoveAll(r => r.uuid == id);
                await HttpServer.Json(context.Response, 200, new { message = "Rating deleted" });
                return;
            }

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }
    }
}
