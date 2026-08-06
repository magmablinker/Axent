using System.Net;
using System.Text.Json;
using Axent.Abstractions.Models;
using Axent.Abstractions.Options;
using NSubstitute;
using Xunit;

namespace Axent.Extensions.Caching.Redis.UnitTests;

public sealed class RedisCacheTest
{
    [Fact]
    public async Task GetOrCreateAsync_should_not_cache_failed_response()
    {
        // Arrange
        var redis = Substitute.For<IRedisConnectionService>();
        var cache = CreateCache(redis);
        var cancellationToken = TestContext.Current.CancellationToken;
        var expectedError = new Error("failure", HttpStatusCode.InternalServerError);
        redis.GetStringAsync(Arg.Any<string>(), cancellationToken)
            .Returns(Task.FromResult<string?>(null));
        redis.TryAcquireLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                cancellationToken)
            .Returns(Task.FromResult(true));
        redis.ReleaseLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                CancellationToken.None)
            .Returns(Task.FromResult(true));

        // Act
        var response = await cache.GetOrCreateAsync(
            "key",
            () => ValueTask.FromResult(Response.Failure<string>(expectedError)),
            cancellationToken: cancellationToken);

        // Assert
        Assert.Same(expectedError, response.Error);
        await redis.DidNotReceiveWithAnyArgs().SetStringAsync(
            default!,
            default!,
            default,
            cancellationToken);
    }

    [Fact]
    public async Task SetAsync_should_store_tag_versions_and_shortest_expiration()
    {
        // Arrange
        var redis = Substitute.For<IRedisConnectionService>();
        var cache = CreateCache(redis);
        var cancellationToken = TestContext.Current.CancellationToken;
        string? storedJson = null;
        TimeSpan? storedExpiration = null;
        redis.GetHashValuesAsync(
                "tests:cache:tag-versions",
                Arg.Is<IReadOnlyList<string>>(tags => tags.SequenceEqual(new[] { "orders" })),
                cancellationToken)
            .Returns(Task.FromResult(new[] { 7L }));
        redis.SetStringAsync(
                "tests:cache:entry:key",
                Arg.Do<string>(value => storedJson = value),
                Arg.Do<TimeSpan?>(value => storedExpiration = value),
                cancellationToken)
            .Returns(Task.FromResult(true));

        // Act
        await cache.SetAsync(
            "key",
            "cached",
            new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(1),
                Tags = ["orders"],
            },
            cancellationToken);

        // Assert
        Assert.NotNull(storedJson);
        Assert.NotNull(storedExpiration);
        Assert.InRange(storedExpiration.Value, TimeSpan.FromSeconds(59), TimeSpan.FromMinutes(1));
        using var document = JsonDocument.Parse(storedJson);
        Assert.Equal("cached", document.RootElement.GetProperty("Value").GetString());
        Assert.Equal(
            7,
            document.RootElement.GetProperty("TagVersions").GetProperty("orders").GetInt64());
    }

    [Fact]
    public async Task GetAsync_should_remove_entry_with_stale_tag_version()
    {
        // Arrange
        var redis = Substitute.For<IRedisConnectionService>();
        var cache = CreateCache(redis);
        var cancellationToken = TestContext.Current.CancellationToken;
        const string json =
            "{\"Value\":\"cached\",\"AbsoluteExpiration\":null,\"SlidingExpiration\":null,\"TagVersions\":{\"orders\":1}}";
        redis.GetStringAsync("tests:cache:entry:key", cancellationToken)
            .Returns(Task.FromResult<string?>(json));
        redis.GetHashValuesAsync(
                "tests:cache:tag-versions",
                Arg.Any<IReadOnlyList<string>>(),
                cancellationToken)
            .Returns(Task.FromResult(new[] { 2L }));

        // Act
        var value = await cache.GetAsync<string>("key", cancellationToken);

        // Assert
        Assert.Null(value);
        await redis.Received(1).DeleteKeyAsync("tests:cache:entry:key", cancellationToken);
    }

    [Fact]
    public async Task RemoveByTagsAsync_should_increment_each_distinct_tag_once()
    {
        // Arrange
        var redis = Substitute.For<IRedisConnectionService>();
        var cache = CreateCache(redis);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await cache.RemoveByTagsAsync(["orders", "orders", "customers"], cancellationToken);

        // Assert
        await redis.Received(1).IncrementHashValuesAsync(
            "tests:cache:tag-versions",
            Arg.Is<IReadOnlyList<string>>(tags => tags.SequenceEqual(new[] { "orders", "customers" })),
            cancellationToken);
    }

    private static RedisCache CreateCache(IRedisConnectionService redis) =>
        new(redis, new RedisOptions { InstanceName = "tests" });
}
