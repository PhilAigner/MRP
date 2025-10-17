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
        private readonly ProfileRepository _profileRepository;
        private readonly TokenService _tokenService;
        private readonly MediaService _mediaService;

        public MediaHTTPEndpoint(MediaRepository mediaRepository, UserRepository userRepository, RatingRepository ratingRepository, ProfileRepository profileRepository, TokenService tokenService)
        {
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
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

            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                // GET doesn't require authentication - public endpoint for browsing
                
                // Special case: Get single media by ID
                var idq = req.QueryString["id"];
                if (!string.IsNullOrWhiteSpace(idq) && Guid.TryParse(idq, out var id))
                {
                    var ent = _mediaRepository.GetMediaById(id);
                    if (ent == null) { await HttpServer.Json(context.Response, 404, new { error = "Media not found" }); return; }
                    await HttpServer.Json(context.Response, 200, ent);
                    return;
                }

                // Start with all media entries
                IEnumerable<MediaEntry> query = _mediaRepository.GetAll();

                // Apply filters progressively (combinable)
                
                // Filter by creator
                var creatorq = req.QueryString["creator"];
                if (!string.IsNullOrWhiteSpace(creatorq) && Guid.TryParse(creatorq, out var creatorId))
                {
                    query = query.Where(m => m.createdBy.uuid == creatorId);
                }

                // Filter by exact title
                var titleq = req.QueryString["title"];
                if (!string.IsNullOrWhiteSpace(titleq))
                {
                    query = query.Where(m => m.title.Equals(titleq, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by title contains
                var titleContainsq = req.QueryString["titleContains"];
                if (!string.IsNullOrWhiteSpace(titleContainsq))
                {
                    query = query.Where(m => m.title.Contains(titleContainsq, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by genre
                var genreq = req.QueryString["genre"];
                if (!string.IsNullOrWhiteSpace(genreq))
                {
                    query = query.Where(m => m.genre.Contains(genreq, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by media type
                var mediaTypeq = req.QueryString["mediaType"];
                if (!string.IsNullOrWhiteSpace(mediaTypeq) && Enum.TryParse<EMediaType>(mediaTypeq, true, out var mediaType))
                {
                    query = query.Where(m => m.mediaType == mediaType);
                }

                // Filter by exact release year
                var releaseYearq = req.QueryString["releaseYear"];
                if (!string.IsNullOrWhiteSpace(releaseYearq) && int.TryParse(releaseYearq, out var releaseYear))
                {
                    query = query.Where(m => m.releaseYear == releaseYear);
                }

                // Filter by minimum release year
                var minYearq = req.QueryString["minYear"];
                if (!string.IsNullOrWhiteSpace(minYearq) && int.TryParse(minYearq, out var minYear))
                {
                    query = query.Where(m => m.releaseYear >= minYear);
                }

                // Filter by maximum release year
                var maxYearq = req.QueryString["maxYear"];
                if (!string.IsNullOrWhiteSpace(maxYearq) && int.TryParse(maxYearq, out var maxYear))
                {
                    query = query.Where(m => m.releaseYear <= maxYear);
                }

                // Filter by age restriction
                var ageRestrictionq = req.QueryString["ageRestriction"];
                if (!string.IsNullOrWhiteSpace(ageRestrictionq) && Enum.TryParse<EFSK>(ageRestrictionq, true, out var ageRestriction))
                {
                    query = query.Where(m => m.ageRestriction == ageRestriction);
                }

                // Filter by minimum average rating
                var minRatingq = req.QueryString["minRating"];
                if (!string.IsNullOrWhiteSpace(minRatingq) && float.TryParse(minRatingq, out var minRating))
                {
                    query = query.Where(m => m.averageScore >= minRating);
                }

                // Filter by maximum average rating
                var maxRatingq = req.QueryString["maxRating"];
                if (!string.IsNullOrWhiteSpace(maxRatingq) && float.TryParse(maxRatingq, out var maxRating))
                {
                    query = query.Where(m => m.averageScore <= maxRating);
                }

                // Convert to list and apply sorting
                var resultList = query.ToList();
                resultList = ApplySorting(resultList, req.QueryString["sortBy"], req.QueryString["sortOrder"]);

                await HttpServer.Json(context.Response, 200, resultList);
                return;
            }

            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Check authentication for creating media
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

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

                    // Verify that the authenticated user is the creator
                    if (createdByGuid != authenticatedUserId)
                    {
                        await AuthenticationHelper.SendForbiddenResponse(context.Response);
                        return;
                    }

                    var user = _userRepository.GetUserById(createdByGuid);
                    if (user == null) { await HttpServer.Json(context.Response, 400, new { error = "createdBy user not found" }); return; }

                    // parse enums
                    if (!Enum.TryParse<EMediaType>(dto.mediaType ?? "Movie", true, out var mtype)) mtype = EMediaType.Movie;
                    if (!Enum.TryParse<EFSK>(dto.ageRestriction ?? "FSK0", true, out var fsk)) fsk = EFSK.FSK0;

                    var entry = new MediaEntry(dto.title!, mtype, dto.releaseYear ?? DateTime.Now.Year, fsk, dto.genre ?? string.Empty, user, dto.description ?? string.Empty);
                    var newId = _mediaService.createMediaEntry(entry);
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
                // Check authentication for updating media
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

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

                    // Verify that the authenticated user is the creator
                    if (existing.createdBy.uuid != authenticatedUserId)
                    {
                        await AuthenticationHelper.SendForbiddenResponse(context.Response);
                        return;
                    }

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
                // Check authentication for deleting media
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                var idq = req.QueryString["id"];
                if (string.IsNullOrWhiteSpace(idq) || !Guid.TryParse(idq, out var id))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'id' query parameter" });
                    return;
                }

                var existing = _mediaRepository.GetMediaById(id);
                if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Media not found" }); return; }

                // Verify that the authenticated user is the creator
                if (existing.createdBy.uuid != authenticatedUserId)
                {
                    await AuthenticationHelper.SendForbiddenResponse(context.Response);
                    return;
                }

                _mediaService.deleteMediaEntry(id);

                await HttpServer.Json(context.Response, 200, new { message = "Media deleted" });
                return;
            }

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }

        private List<MediaEntry> ApplySorting(List<MediaEntry> mediaList, string? sortBy, string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return mediaList;

            bool descending = !string.IsNullOrWhiteSpace(sortOrder) && sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "title" => descending 
                    ? mediaList.OrderByDescending(m => m.title).ToList()
                    : mediaList.OrderBy(m => m.title).ToList(),
                
                "year" or "releaseyear" => descending
                    ? mediaList.OrderByDescending(m => m.releaseYear).ToList()
                    : mediaList.OrderBy(m => m.releaseYear).ToList(),
                
                "score" or "rating" or "averagescore" => descending
                    ? mediaList.OrderByDescending(m => m.averageScore).ToList()
                    : mediaList.OrderBy(m => m.averageScore).ToList(),
                
                _ => mediaList // Invalid sort field, return unsorted
            };
        }
    }
}
