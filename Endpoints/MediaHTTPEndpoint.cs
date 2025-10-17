using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MRP
{
    public sealed class MediaHTTPEndpoint : IHttpEndpoint
    {
        private List<string> paths = new List<string> { "/media", "/mda" };

        private static readonly ConcurrentQueue<string> _store = new();

        public bool CanHandle(HttpListenerRequest request)
        {
            //if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) return false;

            // Normalize path: accept "/user" and "/user/"
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
                if (string.IsNullOrWhiteSpace(userid))
                {
                    await HttpServer.Json(context.Response, 400, new { error = "Missing 'userid' query parameter" });
                    return;
                }

                if (_store.TryDequeue(out var existing))  {         //GET USERDATA TODO
                    await HttpServer.Json(context.Response, 200, existing);
                    return;
                }
                else {
                    await HttpServer.Json(context.Response, 404, new { error = $"No user data for userid '{userid}'" });
                }
            }

            else if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var json = await reader.ReadToEndAsync();
                    var dto = JsonSerializer.Deserialize<string>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto == null || string.IsNullOrWhiteSpace(json.ToString()))
                    {
                        await HttpServer.Json(context.Response, 400, new { error = "Invalid or missing data" });
                        return;
                    }

                    _store.Enqueue(dto.ToString());
                    await HttpServer.Json(context.Response, 201, new { message = $"Added/updated userdata for xy" });
                }
                catch (Exception ex)
                {
                    await HttpServer.Json(context.Response, 400, new { error = $"Invalid JSON: {ex.Message}" });
                }
            }
        }
    }
}
