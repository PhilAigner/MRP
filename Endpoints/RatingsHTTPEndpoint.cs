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
        public bool? publicVisible { get; }
    }

    public sealed class RatingsHTTPEndpoint : IHttpEndpoint
    {
        private readonly RatingRepository _ratingRepository;
        private readonly UserRepository _userRepository;
        private readonly MediaRepository _mediaRepository;
        private readonly ProfileRepository _profileRepository;
        private readonly TokenService _tokenService;
        private readonly RatingService _ratingService;

        public RatingsHTTPEndpoint(RatingRepository ratingRepository, UserRepository userRepository, MediaRepository mediaRepository, ProfileRepository profileRepository, TokenService tokenService)
        {
            _ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _ratingService = new RatingService(_ratingRepository, _profileRepository, _mediaRepository);
        }

        public bool CanHandle(HttpListenerRequest request)
        {
            var path = request.Url!.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            
            // Handle /api/ratings endpoints
            if (path.StartsWith("/api/ratings"))
                return true;
            
            // Handle /api/media/{mediaId}/rate endpoint
            if (path.Contains("/media/") && path.EndsWith("/rate"))
                return true;
                
            return false;
        }

        public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
        {
            var req = context.Request;
            var path = req.Url!.AbsolutePath.TrimEnd('/');

            // Extract ratingId from path: /api/ratings/{ratingId}
            Guid? ratingIdFromPath = null;
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // /api/ratings/{ratingId} or /api/ratings/{ratingId}/like
            if (pathSegments.Length >= 3 && pathSegments[0] == "api" && pathSegments[1] == "ratings")
            {
                if (Guid.TryParse(pathSegments[2], out var parsedGuid))
                {
                    ratingIdFromPath = parsedGuid;
                }
            }

            // Handle /api/media/{mediaId}/rate
            if (path.Contains("/rate") && req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Extract mediaId from /api/media/{mediaId}/rate
                if (pathSegments.Length >= 4 && pathSegments[0] == "api" && pathSegments[1] == "media" && pathSegments[3] == "rate")
                {
                    if (!Guid.TryParse(pathSegments[2], out var mediaId))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Invalid media ID in path" });
                        return;
                    }

                    // Check authentication
                    if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                    {
                        await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                        return;
                    }

                    try
                    {
                        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                        var json = await reader.ReadToEndAsync();
                        var dto = JsonSerializer.Deserialize<RatingDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (dto == null || dto.stars == null)
                        {
                            await HttpServer.Json(context.Response, 400, new { error = "Missing required field: stars" });
                            return;
                        }

                        // User ID from authenticated user
                        var userGuid = authenticatedUserId;

                        // Verify media exists
                        var media = _mediaRepository.GetMediaById(mediaId);
                        if (media == null)
                        {
                            await HttpServer.Json(context.Response, 404, new { error = "Media entry not found" });
                            return;
                        }

                        // Use RatingService to track statistics
                        bool success;
                        if (!string.IsNullOrWhiteSpace(dto.comment))
                        {
                            success = _ratingService.rateMediaEntryWithComment(mediaId, userGuid, dto.stars.Value, dto.comment);
                        }
                        else
                        {
                            success = _ratingService.rateMediaEntry(mediaId, userGuid, dto.stars.Value);
                        }

                        if (!success)
                        {
                            await HttpServer.Json(context.Response, 500, new { error = "Failed to create rating" });
                            return;
                        }

                        // Get the created rating to return its ID
                        var createdRating = _ratingRepository.GetAll()
                            .FirstOrDefault(r => r.mediaEntry == mediaId && r.user == userGuid);
                        


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
            }

            // Handle /api/ratings/{ratingId}/like - POST to like
            if (path.Contains("/like") && req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                if (!ratingIdFromPath.HasValue)
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing rating ID in path" });
                    return;
                }

                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                var success = _ratingService.likeRating(ratingIdFromPath.Value, authenticatedUserId);
                
                if (!success)
                {
                    await HttpServer.Json(context.Response, 409, new { error = "Rating already liked or rating not found" });
                    return;
                }

                await HttpServer.Json(context.Response, 200, new { message = "Rating liked successfully" });
                return;
            }
            
            // Handle /api/ratings/{ratingId}/like - DELETE to unlike
            if (path.Contains("/like") && req.HttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                if (!ratingIdFromPath.HasValue)
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing rating ID in path" });
                    return;
                }

                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                var success = _ratingService.removeLikeFromRating(ratingIdFromPath.Value, authenticatedUserId);
                
                if (!success)
                {
                    await HttpServer.Json(context.Response, 409, new { error = "Rating not liked or rating not found" });
                    return;
                }

                await HttpServer.Json(context.Response, 200, new { message = "Rating unliked successfully" });
                return;
            }
            
            // Handle /api/ratings/{ratingId}/approve - POST to approve a rating
            if (path.Contains("/approve") && req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                if (!ratingIdFromPath.HasValue)
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing rating ID in path" });
                    return;
                }

                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                var success = _ratingService.approveRating(ratingIdFromPath.Value, authenticatedUserId);
                
                if (!success)
                {
                    await HttpServer.Json(context.Response, 403, new { error = "Failed to approve rating. Only the media entry owner can approve ratings." });
                    return;
                }

                await HttpServer.Json(context.Response, 200, new { message = "Rating approved successfully" });
                return;
            }

            // Public endpoint - no authentication required
            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                // Get single rating by ID from path or query
                if (ratingIdFromPath.HasValue)
                {
                    var r = _ratingRepository.GetById(ratingIdFromPath.Value);
                    if (r == null) { await HttpServer.Json(context.Response, 404, new { error = "Rating not found" }); return; }
                    await HttpServer.Json(context.Response, 200, r);
                    return;
                }

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
                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

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

                    // Verify that the authenticated user is creating their own rating
                    if (userGuid != authenticatedUserId)
                    {
                        await AuthenticationHelper.SendForbiddenResponse(context.Response);
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

                    // track statistics
                    bool success;
                    if (!string.IsNullOrWhiteSpace(dto.comment))
                    {
                        success = _ratingService.rateMediaEntryWithComment(mediaGuid, userGuid, dto.stars.Value, dto.comment);
                    }
                    else
                    {
                        success = _ratingService.rateMediaEntry(mediaGuid, userGuid, dto.stars.Value);
                    }

                    if (!success)
                    {
                        await HttpServer.Json(context.Response, 500, new { error = "Failed to create rating" });
                        return;
                    }

                    // Get the created rating to return its ID
                    var createdRating = _ratingRepository.GetAll()
                        .FirstOrDefault(r => r.mediaEntry == mediaGuid && r.user == userGuid);
                    


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

            // PUT: update rating by uuid (path or body)
            if (req.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    var dto = JsonSerializer.Deserialize<RatingDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Get ratingId from path or body
                    Guid uuid;
                    if (ratingIdFromPath.HasValue)
                    {
                        uuid = ratingIdFromPath.Value;
                    }
                    else if (dto != null && !string.IsNullOrWhiteSpace(dto.uuid) && Guid.TryParse(dto.uuid, out var parsedUuid))
                    {
                        uuid = parsedUuid;
                    }
                    else
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'uuid' in path or body" });
                        return;
                    }

                    var existing = _ratingRepository.GetById(uuid);
                    if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Rating not found" }); return; }

                    // Verify that the authenticated user owns this rating
                    if (existing.user != authenticatedUserId)
                    {
                        await AuthenticationHelper.SendForbiddenResponse(context.Response);
                        return;
                    }

                    if (dto != null)
                    {
                        // Track comment changes for review statistics
                        bool hadComment = !string.IsNullOrWhiteSpace(existing.comment);
                        bool willHaveComment = dto.comment != null && !string.IsNullOrWhiteSpace(dto.comment);
                        
                        if (dto.stars != null) existing.stars = dto.stars.Value;
                        if (dto.comment != null) existing.comment = dto.comment;


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

            // DELETE: /api/ratings/{ratingId} or /api/ratings?id=GUID
            if (req.HttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase) && !path.Contains("/like"))
            {
                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                // Get ratingId from path or query
                Guid id;
                if (ratingIdFromPath.HasValue)
                {
                    id = ratingIdFromPath.Value;
                }
                else
                {
                    var idq = req.QueryString["id"];
                    if (string.IsNullOrWhiteSpace(idq) || !Guid.TryParse(idq, out id))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'id' in path or query parameter" });
                        return;
                    }
                }

                var existing = _ratingRepository.GetById(id);
                if (existing == null) { await HttpServer.Json(context.Response, 404, new { error = "Rating not found" }); return; }

                // Verify that the authenticated user owns this rating
                if (existing.user != authenticatedUserId)
                {
                    await AuthenticationHelper.SendForbiddenResponse(context.Response);
                    return;
                }

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
                await HttpServer.Json(context.Response, 204, null);
                return;
            }

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }
    }
}
