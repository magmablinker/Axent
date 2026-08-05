using System.Net;
using Axent.Abstractions.Models;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Axent.Extensions.Caching.UnitTests;

public sealed class InMemoryCacheTest
{
    [Fact]
    public async Task GetOrCreateAsync_should_run_factory_once_for_concurrent_callers()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = new InMemoryCache(memoryCache);
        var cancellationToken = TestContext.Current.CancellationToken;
        var factoryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryCalls = 0;

        async ValueTask<Response<string>> CreateValueAsync()
        {
            Interlocked.Increment(ref factoryCalls);
            factoryStarted.TrySetResult();
            await releaseFactory.Task.WaitAsync(cancellationToken);
            return Response.Success("cached");
        }

        // Act
        var firstCall = cache.GetOrCreateAsync(
            "key",
            CreateValueAsync,
            cancellationToken: cancellationToken).AsTask();
        await factoryStarted.Task.WaitAsync(cancellationToken);

        var remainingCalls = Enumerable.Range(0, 19)
            .Select(_ => cache.GetOrCreateAsync(
                "key",
                CreateValueAsync,
                cancellationToken: cancellationToken).AsTask())
            .ToArray();

        releaseFactory.SetResult();
        var responses = await Task.WhenAll(remainingCalls.Prepend(firstCall));

        // Assert
        Assert.Equal(1, factoryCalls);
        Assert.All(responses, response => Assert.Equal("cached", response.Value));
    }

    [Fact]
    public async Task GetOrCreateAsync_should_not_cache_failed_response()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = new InMemoryCache(memoryCache);
        var cancellationToken = TestContext.Current.CancellationToken;
        var expectedError = new Error("failure", HttpStatusCode.InternalServerError);
        var factoryCalls = 0;

        ValueTask<Response<string>> CreateValueAsync()
        {
            factoryCalls++;
            return ValueTask.FromResult(factoryCalls == 1
                ? Response.Failure<string>(expectedError)
                : Response.Success("recovered"));
        }

        // Act
        var failedResponse = await cache.GetOrCreateAsync(
            "key",
            CreateValueAsync,
            cancellationToken: cancellationToken);
        var successfulResponse = await cache.GetOrCreateAsync(
            "key",
            CreateValueAsync,
            cancellationToken: cancellationToken);

        // Assert
        Assert.Same(expectedError, failedResponse.Error);
        Assert.Equal("recovered", successfulResponse.Value);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_should_allow_waiter_cancellation()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = new InMemoryCache(memoryCache);
        var testCancellationToken = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            testCancellationToken);
        var factoryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<Response<string>> CreateValueAsync()
        {
            factoryStarted.TrySetResult();
            await releaseFactory.Task.WaitAsync(testCancellationToken);
            return Response.Success("cached");
        }

        var owner = cache.GetOrCreateAsync(
            "key",
            CreateValueAsync,
            cancellationToken: testCancellationToken).AsTask();
        await factoryStarted.Task.WaitAsync(testCancellationToken);
        var waiter = cache.GetOrCreateAsync(
            "key",
            CreateValueAsync,
            cancellationToken: cancellation.Token).AsTask();

        // Act
        await cancellation.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);

        releaseFactory.SetResult();
        Assert.Equal("cached", (await owner).Value);
        Assert.Equal(
            "cached",
            (await cache.GetOrCreateAsync(
                "key",
                CreateValueAsync,
                cancellationToken: testCancellationToken)).Value);
    }
}
