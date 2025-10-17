using HttpServerDemo.WeatherServer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MRP
{
    public sealed class HttpServer : IDisposable
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly List<IHttpEndpoint> _endpoints = new List<IHttpEndpoint>();
        private readonly TaskCompletionSource _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completion => _tcs.Task;


        public static async Task RunServer(string prefix, IEnumerable<IHttpEndpoint> endpoints)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

            var server = new HttpServer();
            server.AddPrefix(prefix);
            
            // Alle Endpunkte registrieren
            foreach (var endpoint in endpoints)
            {
                server.RegisterEndpoint(endpoint);
            }

            await server.StartAsync(cts.Token);
            await server.Completion; // waits until cancellation / stop
        }


        // Overload beibehalten für Abwärtskompatibilität
        public static Task RunServer(string prefix, IHttpEndpoint endpoint)
        {
            return RunServer(prefix, new[] { endpoint });
        }

        public void AddPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Prefix must be non-empty.", nameof(prefix));
            if (!prefix.EndsWith("/")) throw new ArgumentException("Prefix must end with a trailing slash.", nameof(prefix));
            _listener.Prefixes.Add(prefix);
        }

        public void RegisterEndpoint(IHttpEndpoint endpoint)
        {
            _endpoints.Add(endpoint ?? throw new ArgumentNullException(nameof(endpoint)));
        }

        public async Task StartAsync(CancellationToken serverCt)
        {
            if (_listener.IsListening) return;

            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            _listener.Start();
            Console.WriteLine("Listening on:");
            foreach (var p in _listener.Prefixes) Console.WriteLine($"  {p}");
            Console.WriteLine("Press Ctrl+C to stop.");

            _ = Task.Run(() => AcceptLoopAsync(serverCt));
        }

        private async Task AcceptLoopAsync(CancellationToken serverCt)
        {
            try
            {
                while (!serverCt.IsCancellationRequested)
                {
                    HttpListenerContext? ctx = await _listener.GetContextAsync(); // Accept a request
                    if (ctx is null) continue;
                    _ = Task.Run(() => DispatchAsync(ctx, serverCt));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Fatal] {ex}");
            }
            finally
            {
                try { _listener.Stop(); } catch { /* ignore */ }
                try { _listener.Close(); } catch { /* ignore */ }
                _tcs.TrySetResult();
                Console.WriteLine("Server stopped.");
            }
        }

        private async Task DispatchAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            var req = ctx.Request;

            // Rudimentary access log
            var started = DateTime.UtcNow;
            try
            {
                foreach (var ep in _endpoints)
                {
                    if (ep.CanHandle(req))
                    {
                        await ep.HandleAsync(ctx, ct);
                        return;
                    }
                }

                await Json(ctx.Response, 404, new { error = "Endpoint not found" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                try { await Json(ctx.Response, 500, new { error = "Internal Server Error" }); }
                catch { /* client may have disconnected */ }
            }
            finally
            {
                var elapsed = DateTime.UtcNow - started;
                Console.WriteLine($"{req.RemoteEndPoint} \"{req.HttpMethod} {req.Url}\" -> {ctx.Response.StatusCode} in {elapsed.TotalMilliseconds:F0} ms");
            }
        }

        #region Helpers (response)
        public static async Task Json(HttpListenerResponse resp, int status, object payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(json);

            resp.StatusCode = status;
            resp.ContentType = "application/json; charset=utf-8";
            resp.ContentLength64 = bytes.Length;

            await resp.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            resp.Close();
        }

        public static async Task Text(HttpListenerResponse resp, int status, string body)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            resp.StatusCode = status;
            resp.ContentType = "text/plain; charset=utf-8";
            resp.ContentLength64 = bytes.Length;

            await resp.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            resp.Close();
        }
        #endregion

        public void Dispose()
        {
            try { _listener.Close(); } catch { }
        }
    }
}
