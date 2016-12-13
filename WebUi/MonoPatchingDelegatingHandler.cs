using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebUi
{
    /// <summary>
    /// Work around a bug in mono's implementation of System.Net.Http where calls to HttpRequestMessage.Headers.Host will fail unless we set it explicitly.
    /// This should be transparent and cause no side effects.
    /// </summary>
    public class MonoPatchingDelegatingHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Host = request.Headers.GetValues("Host").FirstOrDefault();
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}