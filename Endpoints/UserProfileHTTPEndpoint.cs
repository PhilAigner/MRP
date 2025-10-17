using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MRP
{
    // DTO für Profile-Transfer
    public class ProfileDto
    {
        public Guid user { get; set; }
        public int? numberOfLogins { get; set; }
        public int? numberOfRatingsGiven { get; set; }
        public int? numberOfMediaAdded { get; set; }
        public int? numberOfReviewsWritten { get; set; }
        public string? favoriteGenre { get; set; }
        public string? favoriteMediaType { get; set; }
        public string? sobriquet { get; set; }
        public string? aboutMe { get; set; }
    }

    public sealed class UserProfileHTTPEndpoint : IHttpEndpoint
    {
        private readonly List<string> paths = new List<string> { "/api/users/profile" };

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
            foreach (var elm in paths)
            {
                if (path == elm) return true;
            }
            return false;
        }

        public async Task HandleAsync(HttpListenerContext context, CancellationToken ct)
        {
            var req = context.Request;

            // GET: Get profile - requires authentication
            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                // Check authentication
                if (!AuthenticationHelper.RequireAuthentication(req, context.Response, _tokenService, out var authenticatedUserId))
                {
                    await AuthenticationHelper.SendUnauthorizedResponse(context.Response);
                    return;
                }

                var userid = req.QueryString["userid"];
                if (string.IsNullOrWhiteSpace(userid) || !Guid.TryParse(userid, out var userGuid))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'userid' query parameter" });
                    return;
                }

                var profile = _userService.getProfile(userGuid);
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

            // PUT: Update profile - requires authentication and ownership
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

                    if (dto == null || dto.user == Guid.Empty)
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Invalid profile data. 'user' required." });
                        return;
                    }

                    // Check if user is updating their own profile
                    if (dto.user != authenticatedUserId)
                    {
                        await AuthenticationHelper.SendForbiddenResponse(context.Response);
                        return;
                    }

                    // Verify user exists
                    var user = _userRepository.GetUserById(dto.user);
                    if (user == null)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "User not found" });
                        return;
                    }

                    // Get existing profile
                    var existingProfile = _userService.getProfile(dto.user);
                    if (existingProfile == null)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "Profile not found" });
                        return;
                    }

                    // Update only provided fields
                    if (dto.numberOfLogins.HasValue) existingProfile.numberOfLogins = dto.numberOfLogins.Value;
                    if (dto.numberOfRatingsGiven.HasValue) existingProfile.numberOfRatingsGiven = dto.numberOfRatingsGiven.Value;
                    if (dto.numberOfMediaAdded.HasValue) existingProfile.numberOfMediaAdded = dto.numberOfMediaAdded.Value;
                    if (dto.numberOfReviewsWritten.HasValue) existingProfile.numberOfReviewsWritten = dto.numberOfReviewsWritten.Value;
                    if (dto.favoriteGenre != null) existingProfile.favoriteGenre = dto.favoriteGenre;
                    if (dto.favoriteMediaType != null) existingProfile.favoriteMediaType = dto.favoriteMediaType;
                    if (dto.sobriquet != null) existingProfile.sobriquet = dto.sobriquet;
                    if (dto.aboutMe != null) existingProfile.aboutMe = dto.aboutMe;

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

                var userid = req.QueryString["userid"];
                if (string.IsNullOrWhiteSpace(userid) || !Guid.TryParse(userid, out var userGuid))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing or invalid 'userid' query parameter" });
                    return;
                }

                // Check if user is recalculating their own statistics
                if (userGuid != authenticatedUserId)
                {
                    await AuthenticationHelper.SendForbiddenResponse(context.Response);
                    return;
                }

                var profile = _userService.getProfile(userGuid);
                if (profile == null)
                {
                    await HttpServer.Json(context.Response, 404, new { error = "Profile not found" });
                    return;
                }

                // Recalculate all statistics
                _statisticsService.RecalculateStatistics(userGuid);

                await HttpServer.Json(context.Response, 200, new { message = "Statistics recalculated successfully" });
                return;
            }

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }
    }
}
                