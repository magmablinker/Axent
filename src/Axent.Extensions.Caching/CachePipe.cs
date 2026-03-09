using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;

namespace Axent.Extensions.Caching;

internal sealed class CachePipe<TRequest, TResponse>(ICache cache)
    : IAxentPipe<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    public async ValueTask<Response<TResponse>> ProcessAsync(
        IPipelineChain<TRequest, TResponse> chain,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default)
    {
        if (context.Request.BypassCache)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        var value = await cache.GetAsync<TResponse>(context.Request.CacheKey, cancellationToken);
        if (value is not null)
        {
            return Response.Success(value);
        }

        var response = await chain.NextAsync(context, cancellationToken);
        if (!response.IsSuccess || response.Value is null)
        {
            return response;
        }

        await cache.SetAsync(
            context.Request.CacheKey,
            response.Value,
            context.Request.CacheOptions,
            cancellationToken);

        return response;
    }
}
