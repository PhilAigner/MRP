using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MRP
{
    public class RegisterRequest
    {
        public string? username { get; set; }
        public string? password { get; set; }
    }

    public sealed class UserRegisterHTTPEndpoint : IHttpEndpoint
    {
        private readonly List<string> paths = new List<string> { "/api/users/register" };

        private readonly UserRepository _userRepository;
        private readonly ProfileRepository _profileRepository;
        private readonly TokenService _tokenService;
        private readonly UserService _userService;

        public UserRegisterHTTPEndpoint(UserRepository userRepository, ProfileRepository profileRepository, TokenService tokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _userService = new UserService(_userRepository, _profileRepository, _tokenService);
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

            if (!req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
                return;
            }

            try
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var json = await reader.ReadToEndAsync();

                var registerRequest = JsonSerializer.Deserialize<RegisterRequest>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (registerRequest == null || string.IsNullOrWhiteSpace(registerRequest.username) ||
                    string.IsNullOrWhiteSpace(registerRequest.password))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Invalid registration data. 'username' and 'password' required." });
                    return;
                }

                // perform registration
                Guid newUserId = _userService.register(registerRequest.username, registerRequest.password);
                if (newUserId == Guid.Empty)
                {
                    await HttpServer.Json(context.Response, 409, new { error = "Registration failed. Username may already exist." });
                    return;
                }

                // retrieve created user to return its public info
                var createdUser = _userRepository.GetUserByUsername(registerRequest.username);
                if (createdUser == null)
                {
                    await HttpServer.Json(context.Response, 500, new { error = "User created but could not be retrieved." });
                    return;
                }

                await HttpServer.Json(context.Response, 201, new
                {
                    message = "User registered successfully",
                    user = new
                    {
                        uuid = createdUser.uuid,
                        username = createdUser.username,
                        created = createdUser.created,
                        profileUuid = createdUser.profileUuid
                    }
                });
            }
            catch (JsonException ex)
            {
                await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                await HttpServer.Json(context.Response, 500, new { error = $"Server error: {ex.Message}" });
            }
        }
    }
}
                