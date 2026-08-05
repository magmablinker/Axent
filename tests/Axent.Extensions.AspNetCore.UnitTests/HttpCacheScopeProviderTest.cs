using System.Security.Claims;
using Axent.Abstractions.Caching;
using Axent.Core.DependencyInjection;
using Axent.Extensions.AspNetCore.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Extensions.AspNetCore.UnitTests;

public sealed class HttpCacheScopeProviderTest
{
    [Fact]
    public async Task User_scope_should_use_first_available_claim_for_authenticated_user()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "user-42")],
            authenticationType: "test"));

        await using var serviceProvider = BuildServiceProvider(
            principal,
            options => options.UserClaimTypes = ["preferred_user", "sub"]);
        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider
            .GetServices<ICacheScopeProvider>()
            .Single(candidate => candidate.Scope == CacheScope.User);

        // Act
        var discriminator = await provider.GetDiscriminatorAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("user-42", discriminator);
    }

    [Fact]
    public async Task User_scope_should_be_unresolved_for_anonymous_user()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "anonymous-id")]));

        await using var serviceProvider = BuildServiceProvider(principal);
        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider
            .GetServices<ICacheScopeProvider>()
            .Single(candidate => candidate.Scope == CacheScope.User);

        // Act
        var discriminator = await provider.GetDiscriminatorAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(discriminator);
    }

    [Fact]
    public async Task Tenant_scope_should_support_custom_claim_for_anonymous_user()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("workspace", "tenant-west")]));

        await using var serviceProvider = BuildServiceProvider(
            principal,
            options => options.TenantClaimTypes = ["workspace"]);
        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider
            .GetServices<ICacheScopeProvider>()
            .Single(candidate => candidate.Scope == CacheScope.Tenant);

        // Act
        var discriminator = await provider.GetDiscriminatorAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("tenant-west", discriminator);
    }

    private static ServiceProvider BuildServiceProvider(
        ClaimsPrincipal principal,
        Action<HttpCacheScopeOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddAxent(assemblies: []).AddHttpCacheScopes(configure);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
            new DefaultHttpContext { User = principal };

        return serviceProvider;
    }
}
