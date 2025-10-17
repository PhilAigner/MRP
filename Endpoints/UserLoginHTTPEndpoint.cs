using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MRP
{
    // DTO-Klasse für Login-Daten
    public class LoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public sealed class UserLoginHTTPEndpoint : IHttpEndpoint
    {
        private List<string> paths = new List<string> { "/api/users/login", };

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

            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    
                    var loginRequest = JsonSerializer.Deserialize<LoginRequest>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Username) || 
                        string.IsNullOrWhiteSpace(loginRequest.Password))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Invalid login data. Username and password required." });
                        return;
                    }

                    
                    //TODO WIRKLICHER LOGIN PROCESS
                    await HttpServer.Json(context.Response, 200, new { 
                        message = "Login request received",
                        username = loginRequest.Username
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
            else {
                await HttpServer.Json(context.Response, 405, new { error = "Method Not Allowed" });
            }
        }
    }
}
