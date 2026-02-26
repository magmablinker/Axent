using Microsoft.AspNetCore.Http;
using Axent.Abstractions;

namespace Axent.Core;

internal sealed class RequestContextFactory : IRequestContextFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextFactory(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public RequestContext<TRequest> Get<TRequest>(TRequest request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var context = new RequestContext<TRequest>
        {
            Request = request,
        };

        return context;
    }
}