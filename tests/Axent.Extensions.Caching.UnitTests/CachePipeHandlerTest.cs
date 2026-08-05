using Axent.Abstractions.Builders;
using Axent.Abstractions.Caching;
using Axent.Abstractions.Models;
using Axent.Abstractions.Options;
using Axent.Abstractions.Services;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Axent.Extensions.Caching.UnitTests;

public sealed class CachePipeHandlerTest : TestBase
{
    private readonly ICache _mockCache = Substitute.For<ICache>();

    protected override void ConfigureAxent(IAxentBuilder builder)
    {
        builder.AddCache(options =>
        {
            options.EmitScopeTags = true;
            options.UnresolvedScopeBehavior = UnresolvedCacheScopeBehavior.Fail;
        });
        builder.AddCacheKeyProvider<ScopedCacheQuery, TestCacheKeyProvider>();
        builder.AddCacheScopeProvider<TestUserCacheScopeProvider>();
        builder.Services.AddSingleton<ICache>(_ => _mockCache);
    }

    [Fact]
    public async Task SendAsync_should_hit_cache()
    {
        // Arrange
        const string cachedString = "It works!";
        var query = new TestCacheQuery("Hello World!");
        _mockCache.GetOrCreateAsync<string>(
                query.CacheKey,
                Arg.Any<Func<ValueTask<Response<string>>>>(),
                Arg.Any<CacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Response.Success(cachedString));
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query, TestContext.Current.CancellationToken);

        // Assert
        await _mockCache.Received(1).GetOrCreateAsync<string>(
            query.CacheKey,
            Arg.Any<Func<ValueTask<Response<string>>>>(),
            Arg.Any<CacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(cachedString, response.Value);
    }

    [Fact]
    public async Task SendAsync_should_skip_cache()
    {
        // Arrange
        var query = new TestCacheQuery("Bypass", true);
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query, TestContext.Current.CancellationToken);

        // Assert
        await _mockCache.Received(0).GetOrCreateAsync<string>(
            query.CacheKey,
            Arg.Any<Func<ValueTask<Response<string>>>>(),
            Arg.Any<CacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(query.Message, response.Value);
    }

    [Fact]
    public async Task SendAsync_should_compose_provider_key_scope_and_tags()
    {
        // Arrange
        const string expectedKey = "axent:s:u=user%7C42|provided:key";
        var expectedTag = CacheScopeTags.User("user|42");
        var query = new ScopedCacheQuery("cache me");

        _mockCache.GetOrCreateAsync<string>(
                expectedKey,
                Arg.Any<Func<ValueTask<Response<string>>>>(),
                Arg.Is<CacheEntryOptions>(options => options.Tags.SequenceEqual(new[] { expectedTag })),
                Arg.Any<CancellationToken>())
            .Returns(Response.Success("cached"));

        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("cached", response.Value);
        await _mockCache.Received(1).GetOrCreateAsync<string>(
            expectedKey,
            Arg.Any<Func<ValueTask<Response<string>>>>(),
            Arg.Is<CacheEntryOptions>(options => options.Tags.SequenceEqual(new[] { expectedTag })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_should_bypass_cache_when_key_provider_returns_null()
    {
        // Arrange
        var query = new ScopedCacheQuery(TestCacheKeyProvider.SkipMessage);
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(query.Message, response.Value);
        await _mockCache.DidNotReceiveWithAnyArgs().GetOrCreateAsync<string>(
            default!,
            default!,
            default,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_should_fail_when_required_scope_is_unresolved()
    {
        // Arrange
        var query = new ScopedCacheQuery("tenant data", CacheScope.Tenant);
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(CachingErrors.UnresolvedCacheScope(CacheScope.Tenant), response.Error);
        await _mockCache.DidNotReceiveWithAnyArgs().GetOrCreateAsync<string>(
            default!,
            default!,
            default,
            TestContext.Current.CancellationToken);
    }
}

internal sealed class TestCacheKeyProvider : ICacheKeyProvider<ScopedCacheQuery>
{
    public const string SkipMessage = "skip provider";

    public ValueTask<string?> GetCacheKeyAsync(
        ScopedCacheQuery request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<string?>(request.Message == SkipMessage ? null : "provided:key");
}

internal sealed class TestUserCacheScopeProvider : ICacheScopeProvider
{
    public CacheScope Scope => CacheScope.User;

    public ValueTask<string?> GetDiscriminatorAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<string?>("user|42");
}
