using Axent.Abstractions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace Axent.Extensions.Caching;

internal sealed class InMemoryCache(IMemoryCache memoryCache) : ICache
{
    public ValueTask<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (!memoryCache.TryGetValue(key, out var value) || value is not T result)
        {
            return ValueTask.FromResult<T?>(default);
        }

        return ValueTask.FromResult<T?>(result);
    }

    public ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var entryOptions = new MemoryCacheEntryOptions();

        if (options?.AbsoluteExpirationRelativeToNow is not null)
        {
            entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
        }

        if (options?.SlidingExpiration is not null)
        {
            entryOptions.SlidingExpiration = options.SlidingExpiration;
        }

        memoryCache.Set(key, value, entryOptions);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        return ValueTask.CompletedTask;
    }
}
