using System.Text.Json;
using Axent.Abstractions.Models;
using Axent.Abstractions.Options;

namespace Axent.Extensions.Caching.Redis;

internal sealed class RedisCache : ICache
{
    private const string TagVersionsSuffix = "tag-versions";

    private readonly IRedisConnectionService _redis;
    private readonly RedisOptions _options;
    private readonly string _keyPrefix;

    public RedisCache(IRedisConnectionService redis, RedisOptions options)
    {
        _redis = redis;
        _options = options;
        _keyPrefix = $"{options.InstanceName}:cache";
    }

    public async ValueTask<Response<T>> GetOrCreateAsync<T>(
        string key,
        Func<ValueTask<Response<T>>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var entryKey = GetEntryKey(key);
        var lockKey = GetLockKey(key);

        while (true)
        {
            var cachedEntry = await ReadAsync<T>(entryKey, cancellationToken);
            if (cachedEntry.Found)
            {
                return Response.Success(cachedEntry.Value!);
            }

            var lockToken = Guid.NewGuid().ToString("N");
            if (!await _redis.TryAcquireLockAsync(
                    lockKey,
                    lockToken,
                    _options.LockTimeout,
                    cancellationToken))
            {
                await Task.Delay(_options.LockRetryDelay, cancellationToken);
                continue;
            }

            try
            {
                cachedEntry = await ReadAsync<T>(entryKey, cancellationToken);
                if (cachedEntry.Found)
                {
                    return Response.Success(cachedEntry.Value!);
                }

                var response = await factory();
                if (response.IsFailure || response.Value is null)
                {
                    return response;
                }

                await SetEntryAsync(entryKey, response.Value, options, cancellationToken);
                return response;
            }
            finally
            {
                await _redis.ReleaseLockAsync(lockKey, lockToken, CancellationToken.None);
            }
        }
    }

    public async ValueTask<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        var entry = await ReadAsync<T>(GetEntryKey(key), cancellationToken);
        return entry.Value;
    }

    public ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SetEntryAsync(GetEntryKey(key), value, options, cancellationToken);

    public async ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _redis.DeleteKeyAsync(GetEntryKey(key), cancellationToken);
    }

    public async ValueTask RemoveByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        var distinctTags = GetDistinctTags(tags);
        if (distinctTags.Length == 0)
        {
            return;
        }

        await _redis.IncrementHashValuesAsync(
            GetTagVersionsKey(),
            distinctTags,
            cancellationToken);
    }

    private async ValueTask<CacheReadResult<T>> ReadAsync<T>(
        string entryKey,
        CancellationToken cancellationToken)
    {
        var json = await _redis.GetStringAsync(entryKey, cancellationToken);
        if (json is null)
        {
            return default;
        }

        var entry = JsonSerializer.Deserialize<RedisCacheEntry<T>>(json)
            ?? throw new JsonException("Redis cache entry deserialized to null.");
        var utcNow = DateTimeOffset.UtcNow;

        if (entry.AbsoluteExpiration is { } absoluteExpiration && absoluteExpiration <= utcNow)
        {
            await _redis.DeleteKeyAsync(entryKey, cancellationToken);
            return default;
        }

        if (!await HasCurrentTagVersionsAsync(entry.TagVersions, cancellationToken))
        {
            await _redis.DeleteKeyAsync(entryKey, cancellationToken);
            return default;
        }

        if (entry.SlidingExpiration is { } slidingExpiration)
        {
            var expiration = GetExpiration(utcNow, entry.AbsoluteExpiration, slidingExpiration)
                ?? slidingExpiration;
            if (expiration <= TimeSpan.Zero)
            {
                await _redis.DeleteKeyAsync(entryKey, cancellationToken);
                return default;
            }

            await _redis.SetExpirationAsync(entryKey, expiration, cancellationToken);
        }

        return new CacheReadResult<T>(true, entry.Value);
    }

    private async ValueTask SetEntryAsync<T>(
        string entryKey,
        T value,
        CacheEntryOptions? options,
        CancellationToken cancellationToken)
    {
        ValidateExpiration(options?.AbsoluteExpirationRelativeToNow, nameof(options.AbsoluteExpirationRelativeToNow));
        ValidateExpiration(options?.SlidingExpiration, nameof(options.SlidingExpiration));

        var tags = GetDistinctTags(options?.Tags ?? []);
        var tagVersions = await GetTagVersionsAsync(tags, cancellationToken);
        var utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset? absoluteExpiration = options?.AbsoluteExpirationRelativeToNow is { } relativeExpiration
            ? utcNow.Add(relativeExpiration)
            : null;
        var expiration = GetExpiration(utcNow, absoluteExpiration, options?.SlidingExpiration);
        var entry = new RedisCacheEntry<T>(
            value,
            absoluteExpiration,
            options?.SlidingExpiration,
            tagVersions);
        var json = JsonSerializer.Serialize(entry);

        if (!await _redis.SetStringAsync(entryKey, json, expiration, cancellationToken))
        {
            throw new InvalidOperationException("Redis did not store the cache entry.");
        }
    }

    private async ValueTask<bool> HasCurrentTagVersionsAsync(
        IReadOnlyDictionary<string, long> expectedVersions,
        CancellationToken cancellationToken)
    {
        if (expectedVersions.Count == 0)
        {
            return true;
        }

        var tags = expectedVersions.Keys.ToArray();
        var currentVersions = await _redis.GetHashValuesAsync(
            GetTagVersionsKey(),
            tags,
            cancellationToken);

        for (var index = 0; index < tags.Length; index++)
        {
            if (expectedVersions[tags[index]] != currentVersions[index])
            {
                return false;
            }
        }

        return true;
    }

    private async ValueTask<IReadOnlyDictionary<string, long>> GetTagVersionsAsync(
        string[] tags,
        CancellationToken cancellationToken)
    {
        if (tags.Length == 0)
        {
            return new Dictionary<string, long>();
        }

        var versions = await _redis.GetHashValuesAsync(
            GetTagVersionsKey(),
            tags,
            cancellationToken);

        return tags
            .Select((tag, index) => KeyValuePair.Create(tag, versions[index]))
            .ToDictionary(StringComparer.Ordinal);
    }

    private string GetEntryKey(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return $"{_keyPrefix}:entry:{key}";
    }

    private string GetLockKey(string key) => $"{_keyPrefix}:lock:{key}";

    private string GetTagVersionsKey() => $"{_keyPrefix}:{TagVersionsSuffix}";

    private static string[] GetDistinctTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var distinctTags = tags.Distinct(StringComparer.Ordinal).ToArray();
        foreach (var tag in distinctTags)
        {
            ArgumentException.ThrowIfNullOrEmpty(tag);
        }

        return distinctTags;
    }

    private static void ValidateExpiration(TimeSpan? expiration, string parameterName)
    {
        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, expiration, "Expiration must be positive.");
        }
    }

    private static TimeSpan? GetExpiration(
        DateTimeOffset utcNow,
        DateTimeOffset? absoluteExpiration,
        TimeSpan? slidingExpiration)
    {
        var absoluteRemaining = absoluteExpiration - utcNow;
        if (absoluteRemaining is null)
        {
            return slidingExpiration;
        }

        return slidingExpiration is null || absoluteRemaining < slidingExpiration
            ? absoluteRemaining
            : slidingExpiration;
    }

    private readonly record struct CacheReadResult<T>(bool Found, T? Value);

    private sealed record RedisCacheEntry<T>(
        T Value,
        DateTimeOffset? AbsoluteExpiration,
        TimeSpan? SlidingExpiration,
        IReadOnlyDictionary<string, long> TagVersions);
}
