using System.Globalization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Axent.Extensions.Caching.Redis;

internal sealed class RedisConnectionService : IRedisConnectionService
{
    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _database;

    private RedisConnectionService(ConnectionMultiplexer connection)
    {
        _connection = connection;
        _database = connection.GetDatabase();
    }

    public static RedisConnectionService Create(
        ConfigurationOptions configurationOptions,
        ILogger<RedisConnectionService> logger)
    {
        var connection = ConnectionMultiplexer.Connect(configurationOptions);
        logger.MultiplexerConnected();
        return new RedisConnectionService(connection);
    }

    public async Task<string?> GetStringAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key).WaitAsync(cancellationToken);
        return value.HasValue ? value.ToString() : null;
    }

    public Task<bool> SetStringAsync(
        string key,
        string value,
        TimeSpan? expiry,
        CancellationToken cancellationToken = default) =>
        _database.StringSetAsync(key, value, expiry, When.Always).WaitAsync(cancellationToken);

    public Task<bool> DeleteKeyAsync(
        string key,
        CancellationToken cancellationToken = default) =>
        _database.KeyDeleteAsync(key).WaitAsync(cancellationToken);

    public Task<bool> SetExpirationAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        _database.KeyExpireAsync(key, expiry).WaitAsync(cancellationToken);

    public async Task<long[]> GetHashValuesAsync(
        string key,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken = default)
    {
        var redisFields = fields.Select(static field => (RedisValue)field).ToArray();
        var values = await _database.HashGetAsync(key, redisFields).WaitAsync(cancellationToken);

        return [.. values
            .Select(static value => value.HasValue
                ? long.Parse(value.ToString(), CultureInfo.InvariantCulture)
                : 0L)];
    }

    public async Task IncrementHashValuesAsync(
        string key,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken = default)
    {
        var increments = fields
            .Select(field => _database.HashIncrementAsync(key, field))
            .ToArray();

        await Task.WhenAll(increments).WaitAsync(cancellationToken);
    }

    public Task<bool> TryAcquireLockAsync(
        string key,
        string token,
        TimeSpan expiry,
        CancellationToken cancellationToken = default) =>
        _database.LockTakeAsync(key, token, expiry).WaitAsync(cancellationToken);

    public Task<bool> ReleaseLockAsync(
        string key,
        string token,
        CancellationToken cancellationToken = default) =>
        _database.LockReleaseAsync(key, token).WaitAsync(cancellationToken);

    public void Dispose() => _connection.Dispose();
}
