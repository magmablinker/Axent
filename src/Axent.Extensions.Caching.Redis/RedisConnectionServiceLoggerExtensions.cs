using Microsoft.Extensions.Logging;

namespace Axent.Extensions.Caching.Redis;

internal static partial class RedisConnectionServiceLoggerExtensions
{
    [LoggerMessage(EventId = 3000,
        Level = LogLevel.Information,
        Message = "Redis multiplexer connected.")]
    public static partial void MultiplexerConnected(this ILogger<RedisConnectionService> logger);

}
