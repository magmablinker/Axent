using Axent.Abstractions.Builders;
using Axent.Abstractions.Caching;
using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Axent.Extensions.Caching;

public static class AxentBuilderExtensions
{
    public static IAxentBuilder AddCache(
        this IAxentBuilder builder,
        Action<AxentCachingOptions>? configure = null) =>
        builder.AddCache(setup =>
        {
            setup.ConfigureOptions = configure;
            setup.ConfigureCache = cache => cache.UseInMemory();
        });

    public static IAxentBuilder AddCache(
        this IAxentBuilder builder,
        Action<AxentCacheSetup> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var axentCacheSetup = new AxentCacheSetup();
        configure(axentCacheSetup);

        var options = new AxentCachingOptions();
        axentCacheSetup.ConfigureOptions?.Invoke(options);

        var cacheBuilder = new AxentCacheBuilder(builder);
        axentCacheSetup.ConfigureCache ??= cache => cache.UseInMemory();
        axentCacheSetup.ConfigureCache(cacheBuilder);

        builder.Services.AddSingleton(options);
        builder.Services.TryAddScoped<ICacheKeyBuilder, CacheKeyBuilder>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICacheScopeProvider, CultureCacheScopeProvider>());
        builder.Services.AddScoped(typeof(ICachePipe<,>), typeof(CachePipe<,>));

        return builder;
    }

    /// <summary>
    /// Registers a provider that supplies the request identity portion of the cache key for
    /// <typeparamref name="TRequest"/>, replacing its <c>CacheKey</c> property. Use this when the
    /// key needs dependencies. Scope composition still applies on top of the returned value.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    /// <typeparam name="TProvider">Type of the cache key provider</typeparam>
    public static IAxentBuilder AddCacheKeyProvider<TRequest, TProvider>(this IAxentBuilder builder)
        where TProvider : class, ICacheKeyProvider<TRequest>
    {
        builder.Services.AddScoped<ICacheKeyProvider<TRequest>, TProvider>();
        return builder;
    }

    /// <summary>
    /// Registers a provider that resolves the ambient discriminator for a single
    /// <see cref="CacheScope"/> dimension. The last registration for a dimension wins, so this can
    /// override a built-in provider.
    /// </summary>
    /// <typeparam name="TProvider">Type of the cache scope provider</typeparam>
    public static IAxentBuilder AddCacheScopeProvider<TProvider>(this IAxentBuilder builder)
        where TProvider : class, ICacheScopeProvider
    {
        builder.Services.AddScoped<ICacheScopeProvider, TProvider>();
        return builder;
    }
}
