using Axent.Abstractions.Caching;
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

    ValueTask RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default) =>
        RemoveByTagsAsync([tag], cancellationToken);
    
    ValueTask RemoveByTagAsync(
        string tag,
        CacheScope scope,
        string discriminator,
        CancellationToken cancellationToken = default) =>
        RemoveByTagsAsync(
            [CacheScopeTags.ForTag(scope, discriminator, tag)],
            cancellationToken);

    ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}
