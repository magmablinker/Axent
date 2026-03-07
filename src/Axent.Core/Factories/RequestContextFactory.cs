using Axent.Abstractions;

namespace Axent.Core;

internal sealed class RequestContextFactory : IRequestContextFactory
{
    public RequestContext<TRequest> Get<TRequest>(TRequest request)
    {
        var context = new RequestContext<TRequest>
        {
            Request = request,
        };

        return context;
    }
}
