using System.Text;
using Axent.Abstractions.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Axent.Extensions.Caching;

/// <summary>
/// Composes scoped cache keys of the form <c>axent:s:u=42|t=acme|dashboard</c>.
/// </summary>
internal sealed class CacheKeyBuilder : ICacheKeyBuilder
{
    private const string ScopedPrefix = "axent:s:";

    /// <summary>
    /// Dimensions already reported as having no registered provider. A missing provider is a
    /// permanent misconfiguration, so it is logged once per process instead of once per request.
    /// </summary>
    private static int _reportedMissingProviders;

    private readonly ICacheScopeProvider[] _providers;
    private readonly AxentCachingOptions _options;
    private readonly ILogger _logger;

    public CacheKeyBuilder(
        IEnumerable<ICacheScopeProvider> providers,
        AxentCachingOptions options,
        ILogger<CacheKeyBuilder>? logger = null)
    {
        _providers = providers as ICacheScopeProvider[] ?? [.. providers];
        _options = options;
        _logger = logger ?? NullLogger<CacheKeyBuilder>.Instance;
    }

    public async ValueTask<CacheKeyResult> BuildAsync(
        string requestKey,
        CacheScope scope,
        CancellationToken cancellationToken = default)
    {
        var unknown = scope & ~(CacheScope.Global | CacheScopeDimensions.Known);
        if (unknown is not CacheScope.None)
        {
            _logger.UnknownCacheScope(unknown.ToString());
            return CacheKeyResult.Unresolved(unknown);
        }

        var builder = new StringBuilder(ScopedPrefix, requestKey.Length + 32);
        var tags = _options.EmitScopeTags ? new List<string>(CacheScopeDimensions.Ordered.Length) : null;

        foreach (var dimension in CacheScopeDimensions.Ordered)
        {
            if ((scope & dimension) == CacheScope.None)
            {
                continue;
            }

            var provider = FindProvider(dimension);
            if (provider is null)
            {
                ReportMissingProviderOnce(dimension);
                return CacheKeyResult.Unresolved(dimension);
            }

            var discriminator = await provider.GetDiscriminatorAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(discriminator))
            {
                _logger.CacheScopeDiscriminatorUnavailable(
                    dimension.ToString(),
                    provider.GetType().Name);

                return CacheKeyResult.Unresolved(dimension);
            }

            var escaped = CacheKeyEscaper.Escape(discriminator);

            builder.Append(CacheScopeDimensions.Abbreviate(dimension));
            builder.Append('=');
            builder.Append(escaped);
            builder.Append('|');

            tags?.Add(CacheScopeTags.For(dimension, discriminator));
        }

        // The request key is the final segment, so it never needs escaping.
        builder.Append(requestKey);

        return CacheKeyResult.Resolved(builder.ToString(), tags ?? []);
    }

    /// <summary>
    /// Last matching registration wins, so a consumer's own provider overrides a built-in one.
    /// </summary>
    private ICacheScopeProvider? FindProvider(CacheScope dimension)
    {
        for (var index = _providers.Length - 1; index >= 0; index--)
        {
            if (_providers[index].Scope == dimension)
            {
                return _providers[index];
            }
        }

        return null;
    }

    private void ReportMissingProviderOnce(CacheScope dimension)
    {
        var flag = (int)dimension;
        var previous = Interlocked.Or(ref _reportedMissingProviders, flag);

        if ((previous & flag) == 0)
        {
            _logger.CacheScopeProviderMissing(dimension.ToString());
        }
    }
}

internal static partial class CacheKeyBuilderLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "No ICacheScopeProvider is registered for cache scope '{scope}'. "
                  + "Scoped queries requiring it will bypass the cache.")]
    public static partial void CacheScopeProviderMissing(this ILogger logger, string scope);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Provider '{providerType}' returned no discriminator for cache scope '{scope}'. "
                  + "Bypassing the cache for this request.")]
    public static partial void CacheScopeDiscriminatorUnavailable(
        this ILogger logger, string scope, string providerType);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Cache scope '{scope}' is not a known dimension. Bypassing the cache for this request.")]
    public static partial void UnknownCacheScope(this ILogger logger, string scope);
}
