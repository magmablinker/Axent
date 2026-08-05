using Axent.Abstractions.Models;
using Axent.Abstractions.Options;

namespace Axent.Extensions.Caching;

public interface ICache
{
    ValueTask<Response<T>> GetOrCreateAsync<T>(
        string key,
        Func<ValueTask<Response<T>>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
    ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellation = default);
}
