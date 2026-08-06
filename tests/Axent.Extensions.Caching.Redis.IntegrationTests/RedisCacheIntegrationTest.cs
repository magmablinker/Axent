using Axent.Abstractions.Models;
using Axent.Abstractions.Options;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;
using Xunit;

namespace Axent.Extensions.Caching.Redis.IntegrationTests;

public sealed class RedisCacheIntegrationTest(RedisCacheFixture fixture)
    : IClassFixture<RedisCacheFixture>
{
    [Fact]
    public async Task Set_get_and_remove_should_round_trip_value()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new CachedOrder(42, "ready");

        // Act
        await fixture.Cache.SetAsync("order:42", expected, cancellationToken: cancellationToken);
        var cached = await fixture.Cache.GetAsync<CachedOrder>("order:42", cancellationToken);
        await fixture.Cache.RemoveAsync("order:42", cancellationToken);

        // Assert
        Assert.Equal(expected, cached);
        Assert.Null(await fixture.Cache.GetAsync<CachedOrder>("order:42", cancellationToken));
    }

    [Fact]
    public async Task RemoveByTagAsync_should_invalidate_only_matching_entries()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.Cache.SetAsync(
            "order:1",
            "first",
            new CacheEntryOptions { Tags = ["orders"] },
            cancellationToken);
        await fixture.Cache.SetAsync(
            "order:2",
            "second",
            new CacheEntryOptions { Tags = ["orders", "featured"] },
            cancellationToken);
        await fixture.Cache.SetAsync(
            "customer:1",
            "customer",
            new CacheEntryOptions { Tags = ["customers"] },
            cancellationToken);

        // Act
        await fixture.Cache.RemoveByTagAsync("orders", cancellationToken);

        // Assert
        Assert.Null(await fixture.Cache.GetAsync<string>("order:1", cancellationToken));
        Assert.Null(await fixture.Cache.GetAsync<string>("order:2", cancellationToken));
        Assert.Equal("customer", await fixture.Cache.GetAsync<string>("customer:1", cancellationToken));
    }

    [Fact]
    public async Task GetOrCreateAsync_should_run_factory_once_for_concurrent_callers()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var factoryCalls = 0;

        async ValueTask<Response<string>> CreateValueAsync()
        {
            Interlocked.Increment(ref factoryCalls);
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return Response.Success("cached");
        }

        // Act
        var calls = Enumerable.Range(0, 20)
            .Select(_ => fixture.Cache.GetOrCreateAsync(
                "concurrent",
                CreateValueAsync,
                cancellationToken: cancellationToken).AsTask());
        var responses = await Task.WhenAll(calls);

        // Assert
        Assert.Equal(1, factoryCalls);
        Assert.All(responses, response => Assert.Equal("cached", response.Value));
    }

    [Fact]
    public async Task Absolute_expiration_should_remove_entry()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.Cache.SetAsync(
            "absolute",
            "cached",
            new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(300),
            },
            cancellationToken);

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);

        // Assert
        Assert.Null(await fixture.Cache.GetAsync<string>("absolute", cancellationToken));
    }

    [Fact]
    public async Task Sliding_expiration_should_renew_on_access()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.Cache.SetAsync(
            "sliding",
            "cached",
            new CacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(1) },
            cancellationToken);

        // Act and assert
        await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);
        Assert.Equal("cached", await fixture.Cache.GetAsync<string>("sliding", cancellationToken));
        await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);
        Assert.Equal("cached", await fixture.Cache.GetAsync<string>("sliding", cancellationToken));
        await Task.Delay(TimeSpan.FromMilliseconds(1200), cancellationToken);
        Assert.Null(await fixture.Cache.GetAsync<string>("sliding", cancellationToken));
    }

    private sealed record CachedOrder(int Id, string Status);

}

public sealed class RedisCacheFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();
    private ServiceProvider? _serviceProvider;

    public ICache Cache { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new TestCacheBuilder(services);
        builder.UseRedis(options =>
        {
            options.ConnectionString = _container.GetConnectionString();
            options.InstanceName = $"axent-tests-{Guid.NewGuid():N}";
            options.LockRetryDelay = TimeSpan.FromMilliseconds(10);
        });

        _serviceProvider = services.BuildServiceProvider();
        Cache = _serviceProvider.GetRequiredService<ICache>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private sealed class TestCacheBuilder(IServiceCollection services) : IAxentCacheBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
