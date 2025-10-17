using HttpServerDemo.WeatherServer;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MRP
{
    public sealed class WeatherServiceEndpoint : IHttpEndpoint
    {
        private List<string> paths = new List<string> { "/weather", "/test" };
 
        public bool CanHandle(HttpListenerRequest request)
        {
            if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) return false;

            // Normalize path: accept "/weather" and "/weather/"
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

            var city = req.QueryString["city"];
            if (string.IsNullOrWhiteSpace(city))
            {
                await HttpServer.Json(context.Response, 400, new { error = "Missing 'city' query parameter" });
                return;
            }

            // Lookup mock data; replace with real provider if needed
            var data = Lookup(city);
            if (data is null)
            {
                await HttpServer.Json(context.Response, 404, new { error = $"No weather data for city '{city}'" });
                return;
            }

            await HttpServer.Json(context.Response, 200, data);
        }

        private object? Lookup(string city)
        {
            // Demo-only: static cases. Extend to call a DB or an external API.
            var lc = city.Trim().ToLowerInvariant();
            return lc switch
            {
                "vienna" or "wien" => new WeatherDto("Vienna", 23.5, "Sunny", DateTime.UtcNow),
                "london"            => new WeatherDto("London", 17.2, "Cloudy", DateTime.UtcNow),
                "new york"          => new WeatherDto("New York", 28.1, "Partly Cloudy", DateTime.UtcNow),
                _ => null
            };
        }

        private sealed record WeatherDto(
            string city,
            double temperatureCelsius,
            string condition,
            DateTime timeUtc
        );
    }
}
