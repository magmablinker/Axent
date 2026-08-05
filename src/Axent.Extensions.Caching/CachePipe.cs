using Axent.Abstractions.Caching;
using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;

namespace Axent.Extensions.Caching;

internal sealed class CachePipe<TRequest, TResponse>(
    ICache cache,
    ICacheKeyBuilder keyBuilder,
    AxentCachingOptions options,
    ICacheKeyProvider<TRequest>? keyProvider = null)
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

        var requestKey = request.CacheKey;

        if (keyProvider is not null)
        {
            requestKey = await keyProvider.GetCacheKeyAsync(request, cancellationToken);

            if (requestKey is null)
            {
                return await next(request, cancellationToken);
            }
        }

        var scope = request.CacheScope;

        if (scope is CacheScope.Global)
        {
            return await cache.GetOrCreateAsync(
                requestKey,
                () => next(request, cancellationToken),
                request.CacheOptions,
                cancellationToken);
        }

        var keyResult = await keyBuilder.BuildAsync(requestKey, scope, cancellationToken);

        if (!keyResult.IsResolved)
        {
            return options.UnresolvedScopeBehavior is UnresolvedCacheScopeBehavior.Fail
                ? Response.Failure<TResponse>(CachingErrors.UnresolvedCacheScope(keyResult.UnresolvedScope))
                : await next(request, cancellationToken);
        }

        var entryOptions = keyResult.ScopeTags.Count is 0
            ? request.CacheOptions
            : request.CacheOptions with { Tags = [.. request.CacheOptions.Tags, .. keyResult.ScopeTags] };

        return await cache.GetOrCreateAsync(
            keyResult.Key,
            () => next(request, cancellationToken),
            entryOptions,
            cancellationToken);
    }
}
