using Axent.Abstractions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Axent.Extensions.Caching;

internal sealed class InMemoryCache(IMemoryCache memoryCache) : ICache, IDisposable
{
    private static readonly StringComparer _tagComparer = StringComparer.Ordinal;

    private readonly SemaphoreSlim _tagSemaphore = new(1, 1);
    private readonly Dictionary<string, CancellationTokenSource> _tagTokens = new(_tagComparer);

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

    public async ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var entryOptions = await CreateMemoryCacheEntryOptionsAsync(options, cancellationToken);

        memoryCache.Set(key, value, entryOptions);
    }

    public ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        return ValueTask.CompletedTask;
    }

    public async ValueTask RemoveByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        List<CancellationTokenSource> tokensToCancel = [];

        await _tagSemaphore.WaitAsync(cancellationToken);

        try
        {
            foreach (var tag in NormalizeTags(tags))
            {
                if (!_tagTokens.Remove(tag, out var currentTokenSource))
                {
                    continue;
                }

                tokensToCancel.Add(currentTokenSource);
            }
        }
        finally
        {
            _tagSemaphore.Release();
        }

        foreach (var tokenSource in tokensToCancel)
        {
            try
            {
                await tokenSource.CancelAsync();
            }
            finally
            {
                tokenSource.Dispose();
            }
        }
    }

    private async ValueTask<MemoryCacheEntryOptions> CreateMemoryCacheEntryOptionsAsync(
        CacheEntryOptions? options,
        CancellationToken cancellationToken)
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

        if (options?.Tags.Count > 0)
        {
            await AddTagExpirationTokensAsync(entryOptions, options.Tags, cancellationToken);
        }

        return entryOptions;
    }

    private async ValueTask AddTagExpirationTokensAsync(
        MemoryCacheEntryOptions entryOptions,
        IEnumerable<string> tags,
        CancellationToken cancellationToken)
    {
        await _tagSemaphore.WaitAsync(cancellationToken);

        try
        {
            foreach (var tag in NormalizeTags(tags))
            {
                if (!_tagTokens.TryGetValue(tag, out var tokenSource))
                {
                    tokenSource = new CancellationTokenSource();
                    _tagTokens[tag] = tokenSource;
                }

                entryOptions.ExpirationTokens.Add(
                    new CancellationChangeToken(tokenSource.Token));
            }
        }
        finally
        {
            _tagSemaphore.Release();
        }
    }

    private static IEnumerable<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(_tagComparer);
    }

    public void Dispose()
    {
        _tagSemaphore.Wait();

        try
        {
            foreach (var tokenSource in _tagTokens.Values)
            {
                tokenSource.Dispose();
            }

            _tagTokens.Clear();
        }
        finally
        {
            _tagSemaphore.Release();
            _tagSemaphore.Dispose();
        }
    }
}
