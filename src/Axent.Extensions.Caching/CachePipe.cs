using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;

namespace Axent.Extensions.Caching;

internal sealed class CachePipe<TRequest, TResponse>(ICache cache)
    : ICachePipe<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    public async ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        if (request.BypassCache)
        {
            return await next(request, cancellationToken);
        }

        return await cache.GetOrCreateAsync(
            request.CacheKey,
            () => next(request, cancellationToken),
            request.CacheOptions,
            cancellationToken);
    }
}
