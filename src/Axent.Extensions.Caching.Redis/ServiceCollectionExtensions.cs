using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Axent.Extensions.Caching.Redis;

public static class ServiceCollectionExtensions
{
    extension(IAxentCacheBuilder builder)
    {
        public IAxentCacheBuilder UseRedis(Action<RedisOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var options = new RedisOptions();
            configure(options);

            ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.InstanceName);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.LockTimeout, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.LockRetryDelay, TimeSpan.Zero);

            var configurationOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(1000);
            configurationOptions.ClientName = CreateClientName();
            configurationOptions.AbortOnConnectFail = false;

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<IRedisConnectionService>(provider =>
                RedisConnectionService.Create(configurationOptions, provider.GetRequiredService<ILogger<RedisConnectionService>>()));

            builder.Services.AddSingleton<ICache, RedisCache>();

            return builder;
        }
    }

    private static string CreateClientName()
    {
        var machineName = Environment.MachineName;
        var serviceName = typeof(ServiceCollectionExtensions).Assembly.GetName().Name;
        return $"{serviceName}-{machineName}";
    }
}
