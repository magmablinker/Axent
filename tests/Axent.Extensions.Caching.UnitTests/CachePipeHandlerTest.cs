using Axent.Abstractions.Builders;
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
        builder.AddCache();
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
}
