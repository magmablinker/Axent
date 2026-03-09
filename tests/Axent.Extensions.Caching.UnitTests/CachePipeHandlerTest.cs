using Axent.Abstractions.Builders;
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
        _mockCache.GetAsync<string>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns(cachedString);
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        await _mockCache.Received(1).GetAsync<string>(query.CacheKey, Arg.Any<CancellationToken>());
        Assert.Equal(cachedString, response.Value);
    }

    [Fact]
    public async Task SendAsync_should_skip_cache()
    {
        // Arrange
        const string cachedString = "It works!";
        var query = new TestCacheQuery("Bypass", true);
        _mockCache.GetAsync<string>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns(cachedString);
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        await _mockCache.Received(0).GetAsync<string>(query.CacheKey, Arg.Any<CancellationToken>());
        Assert.Equal(query.Message, response.Value);
    }
}
