using Axent.Abstractions.Requests;

namespace Axent.Abstractions.Caching;

/// <summary>
/// Supplies the request identity portion of the cache key for <typeparamref name="TRequest"/>,
/// replacing <see cref="ICacheableQuery{TResponse}.CacheKey"/>.
/// </summary>
/// <remarks>
/// Scope composition still applies on top of the returned value, so implementing this
/// interface cannot lose the partitioning declared by
/// <see cref="ICacheableQuery{TResponse}.CacheScope"/>.
/// </remarks>
/// <typeparam name="TRequest">Type of the request</typeparam>
public interface ICacheKeyProvider<in TRequest>
{
    /// <summary>
    /// Returns the request identity portion of the cache key,
    /// or <c>null</c> to skip caching for this request.
    /// </summary>
    /// <param name="request">The request being sent</param>
    /// <param name="cancellationToken">Token used to cancel the operation</param>
    ValueTask<string?> GetCacheKeyAsync(TRequest request, CancellationToken cancellationToken = default);
}
