using System.Net;
using System.Text.Json;

namespace MRP
{
    // DTO für Profile - only fields that can be set from outside
    public class ProfileDto
    {
        public string? sobriquet { get; set; }
        public string? aboutMe { get; set; }
    }


    public sealed class UserProfileHTTPEndpoint : IHttpEndpoint
    {
        private readonly UserRepository _userRepository;
        private readonly ProfileRepository _profileRepository;
        private readonly TokenService _tokenService;
        private readonly UserService _userService;
        private readonly ProfileStatisticsService _statisticsService;

        public UserProfileHTTPEndpoint(UserRepository userRepository, ProfileRepository profileRepository, RatingRepository ratingRepository, MediaRepository mediaRepository, TokenService tokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _userService = new UserService(_userRepository, _profileRepository, _tokenService);
            _statisticsService = new ProfileStatisticsService(_profileRepository, ratingRepository, mediaRepository);
        }

        public bool CanHandle(HttpListenerRequest request)
        {
            var path = request.Url!.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            
            // Handle /api/users/{userId}/profile
            if (path.Contains("/users/") && path.EndsWith("/profile"))
                return true;
            
            // Handle legacy /api/users/profile
            if (path == "/api/users/profile")
                return true;
            
            return false;
        }

        public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
        {
            var req = context.Request;
            var path = req.Url!.AbsolutePath.TrimEnd('/');

            // Extract userId from path: /api/users/{userId}/profile
            Guid? userIdFromPath = null;
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length >= 4 && pathSegments[0] == "api" && pathSegments[1] == "users" && pathSegments[3] == "profile")
            {
                if (Guid.TryParse(pathSegments[2], out var parsedGuid))
                {
                    userIdFromPath = parsedGuid;
                }
            }

            // GET: Get profile - requires authentication
            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                Guid userId;
                if (userIdFromPath.HasValue)
                {
                    userId = userIdFromPath.Value;
                }
                else
                {
                    var userid = req.QueryString["userid"];
                    if (string.IsNullOrWhiteSpace(userid) || !Guid.TryParse(userid, out userId))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'userid' in path or query parameter" });
                        return;
                    }
                }

                var profile = _userService.getProfile(userId);
                if (profile == null)
                {
                    await HttpServer.Json(context.Response, 404, new { error = "Profile not found" });
                    return;
                }

                await HttpServer.Json(context.Response, 200, new
                {
                    uuid = profile.uuid,
                    user = profile.user,
                    numberOfLogins = profile.numberOfLogins,
                    numberOfRatingsGiven = profile.numberOfRatingsGiven,
                    numberOfMediaAdded = profile.numberOfMediaAdded,
                    numberOfReviewsWritten = profile.numberOfReviewsWritten,
                    favoriteGenre = profile.favoriteGenre,
                    favoriteMediaType = profile.favoriteMediaType,
                    sobriquet = profile.sobriquet,
                    aboutMe = profile.aboutMe
                });

                return;
            }

            // PUT: Update profile | NEEDS to be logged in and ownership &&  only sobriquet and about can be updated
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
                    var dto = JsonSerializer.Deserialize<ProfileDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Get userId from path
                    Guid userId;
                    if (userIdFromPath.HasValue)
                    {
                        userId = userIdFromPath.Value;
                    }
                    else
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "User ID must be specified in path" });
                        return;
                    }

                    // Verify user exists FIRST (before checking ownership)
                    var user = _userRepository.GetUserById(userId);
                    if (user == null)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "User not found" });
                        return;
                    }

                    // Get if profile exists
                    var existingProfile = _userService.getProfile(userId);
                    if (existingProfile == null)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "Profile not found" });
                        return;
                    }

                    // check if user is updating their own profile
                    if (userId != authenticatedUserId)
                    {
                        await HttpServer.Json(context.Response, 403, new { 
                            error = "Forbidden. You can only update your own profile.", 
                            detail = $"Token userId: {authenticatedUserId}, Profile userId: {userId}" 
                        });
                        return;
                    }

                    if (dto != null)
                    {
                        // Update only sobriquet and aboutMe
                        if (dto.sobriquet != null) existingProfile.sobriquet = dto.sobriquet;
                        if (dto.aboutMe != null) existingProfile.aboutMe = dto.aboutMe;
                    }

                    var ok = _userService.editProfile(existingProfile);
                    if (!ok)
                    {
                        await HttpServer.Json(context.Response, 500, new { error = "Profile could not be updated." });
                        return;
                    }

                    await HttpServer.Json(context.Response, 200, new { message = "Profile updated" });
                }
                catch (JsonException ex)
                {
                    await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON format: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    await HttpServer.Json(context.Response, 500, new { error = $"Server error: {ex.Message}" });
                }

                return;
            }

            // POST: Recalculate statistics - requires authentication and ownership
            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                // Get userId from path or query
                Guid userId;
                if (userIdFromPath.HasValue)
                {
                    userId = userIdFromPath.Value;
                }
                else
                {
                    var userid = req.QueryString["userid"];
                    if (string.IsNullOrWhiteSpace(userid) || !Guid.TryParse(userid, out userId))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'userid' in path or query parameter" });
                        return;
                    }
                }

                // Check if user is recalculating their own statistics
                if (userId != authenticatedUserId)
                {
                    await AuthenticationHelper.SendForbiddenResponse(context.Response);
                    return;
                }

                var profile = _userService.getProfile(userId);
                if (profile == null)
                {
                    await HttpServer.Json(context.Response, 404, new { error = "Profile not found" });
                    return;
                }

                // Recalculate all statistics
                _statisticsService.RecalculateStatistics(userId);

                await HttpServer.Json(context.Response, 200, new { message = "Statistics recalculated successfully" });
                return;
            }

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }
    }
}