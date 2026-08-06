using Axent.Abstractions.Builders;
using Axent.Abstractions.Caching;
using Axent.Extensions.AspNetCore.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Axent.Extensions.AspNetCore;

public static class AxentBuilderExtensions
{
    /// <summary>
    /// Registers claims-backed providers for <see cref="CacheScope.User"/> and
    /// <see cref="CacheScope.Tenant"/>, read from the current <c>HttpContext</c>.
    /// </summary>
    /// <param name="builder">The Axent builder</param>
    /// <param name="configure">Optional configuration of which claims are read</param>
    public static IAxentBuilder AddHttpCacheScopes(
        this IAxentBuilder builder,
        Action<HttpCacheScopeOptions>? configure = null)
    {
        var options = new HttpCacheScopeOptions();
        configure?.Invoke(options);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(options);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICacheScopeProvider, HttpUserCacheScopeProvider>());
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICacheScopeProvider, HttpTenantCacheScopeProvider>());

        return builder;
    }
}
