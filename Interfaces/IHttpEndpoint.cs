using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MRP;

public interface IHttpEndpoint
{
    bool CanHandle(HttpListenerRequest request);
    Task HandleAsync(HttpListenerContext context, CancellationToken ct);
}
