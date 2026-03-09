using Axent.Abstractions.Factories;
using Axent.Abstractions.Models;

namespace Axent.Core.Factories;

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
