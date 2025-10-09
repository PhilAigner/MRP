using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServerDemo.WeatherServer
{
    public interface IHttpEndpoint
    {
        bool CanHandle(HttpListenerRequest request);
        Task HandleAsync(HttpListenerContext context, CancellationToken ct);
    }
}
