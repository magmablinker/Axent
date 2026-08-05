using Axent.Abstractions.Caching;

namespace Axent.Extensions.Caching;

/// <summary>
/// Composes the final cache key from a request key and the ambient discriminators
/// required by a <see cref="CacheScope"/>.
/// </summary>
public interface ICacheKeyBuilder
{
    /// <summary>
    /// Composes the cache key for <paramref name="requestKey"/> in <paramref name="scope"/>.
    /// </summary>
    /// <param name="requestKey">The request identity portion of the key</param>
    /// <param name="scope">Ambient dimensions the entry must be partitioned by</param>
    /// <param name="cancellationToken">Token used to cancel the operation</param>
    ValueTask<CacheKeyResult> BuildAsync(
        string requestKey,
        CacheScope scope,
        CancellationToken cancellationToken = default);
}
