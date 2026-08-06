namespace Axent.Extensions.Caching.Redis;

internal interface IRedisConnectionService : IDisposable
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> SetStringAsync(
        string key,
        string value,
        TimeSpan? expiry,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> SetExpirationAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<long[]> GetHashValuesAsync(
        string key,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken = default);

    Task IncrementHashValuesAsync(
        string key,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken = default);

    Task<bool> TryAcquireLockAsync(
        string key,
        string token,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseLockAsync(
        string key,
        string token,
        CancellationToken cancellationToken = default);
}
