using System.Net;

namespace MRP;

public interface IHttpEndpoint
{
    bool CanHandle(HttpListenerRequest request);
    Task HandleAsync(HttpListenerContext context, CancellationToken ct);
}