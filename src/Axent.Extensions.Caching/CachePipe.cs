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

        var value = await cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (value is not null)
        {
            return Response.Success(value);
        }

        var response = await next(request, cancellationToken);
        if (!response.IsSuccess || response.Value is null)
        {
            return response;
        }

        await cache.SetAsync(
            request.CacheKey,
            response.Value,
            request.CacheOptions,
            cancellationToken);

        return response;
    }
}
