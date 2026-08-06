using Axent.Abstractions.Caching;
using Axent.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Extensions.Caching.UnitTests;

public sealed class CacheKeyBuilderTest
{
    [Fact]
    public async Task BuildAsync_should_compose_dimensions_in_stable_order_and_emit_tags()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddAxent(assemblies: []);
        builder.AddCache(options => options.EmitScopeTags = true);
        builder.Services.AddScoped<ICacheScopeProvider>(
            _ => new FixedScopeProvider(CacheScope.Tenant, "tenant:west"));
        builder.Services.AddScoped<ICacheScopeProvider>(
            _ => new FixedScopeProvider(CacheScope.User, "user|42"));
        builder.Services.AddScoped<ICacheScopeProvider>(
            _ => new FixedScopeProvider(CacheScope.Culture, "de-CH"));

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var keyBuilder = scope.ServiceProvider.GetRequiredService<ICacheKeyBuilder>();

        // Act
        var result = await keyBuilder.BuildAsync(
            "orders:7",
            CacheScope.Culture | CacheScope.Tenant | CacheScope.User,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(
            result.IsResolved,
            $"Resolved {keyBuilder.GetType().FullName} with {result.UnresolvedScope}.");
        Assert.Equal(
            "axent:s:u=user%7C42|t=tenant%3Awest|c=de-CH|orders:7",
            result.Key);
        Assert.Equal(
            [
                CacheScopeTags.User("user|42"),
                CacheScopeTags.Tenant("tenant:west"),
                CacheScopeTags.For(CacheScope.Culture, "de-CH"),
            ],
            result.ScopeTags);
    }

    [Fact]
    public async Task BuildAsync_should_report_unresolved_dimension_when_provider_is_missing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAxent(assemblies: []).AddCache();

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var keyBuilder = scope.ServiceProvider.GetRequiredService<ICacheKeyBuilder>();

        // Act
        var result = await keyBuilder.BuildAsync(
            "orders",
            CacheScope.User,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsResolved);
        Assert.Null(result.Key);
        Assert.Equal(CacheScope.User, result.UnresolvedScope);
        Assert.Empty(result.ScopeTags);
    }

    private sealed class FixedScopeProvider(CacheScope scope, string discriminator)
        : ICacheScopeProvider
    {
        public CacheScope Scope { get; } = scope;

        public ValueTask<string?> GetDiscriminatorAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<string?>(discriminator);
    }
}
