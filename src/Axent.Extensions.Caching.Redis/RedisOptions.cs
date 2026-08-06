namespace Axent.Extensions.Caching.Redis;

public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = "axent";

    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan LockRetryDelay { get; set; } = TimeSpan.FromMilliseconds(50);
}
