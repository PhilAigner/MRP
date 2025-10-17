using System;
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
        public int someStatistic { get; set; }
    }

    public sealed class UserProfileHTTPEndpoint : IHttpEndpoint
    {
        private readonly List<string> paths = new List<string> { "/api/users/profile" };

        private readonly UserRepository _userRepository;
        private readonly ProfileRepository _profileRepository;
        private readonly UserService _userService;

        public UserProfileHTTPEndpoint(UserRepository userRepository, ProfileRepository profileRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _userService = new UserService(_userRepository, _profileRepository);
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

            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
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
                    someStatistic = profile.someStatistic
                });

                return;
            }

            if (req.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
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

                    // build new profile instance and set values
                    var newProfile = new Profile(dto.user);
                    newProfile.someStatistic = dto.someStatistic;

                    var ok = _userService.editProfile(newProfile);
                    if (!ok)
                    {
                        await HttpServer.Json(context.Response, 404, new { error = "Profile not found or could not be updated." });
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

            await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
        }
    }
}
                