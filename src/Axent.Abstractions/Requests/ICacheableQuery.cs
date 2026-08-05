using Axent.Abstractions.Caching;
using Axent.Abstractions.Options;

namespace Axent.Abstractions.Requests;

/// <summary>
/// Marker interface for cacheable queries
/// </summary>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface ICacheableQuery<out TResponse> : IQuery<TResponse>
{
    string CacheKey { get; }
    bool BypassCache { get; }
    CacheEntryOptions CacheOptions => new();

    /// <summary>
    /// Ambient dimensions this entry must be partitioned by.
    /// <see cref="Caching.CacheScope.Global"/>, the default, shares one entry across all callers.
    /// </summary>
    CacheScope CacheScope => Caching.CacheScope.Global;
}
