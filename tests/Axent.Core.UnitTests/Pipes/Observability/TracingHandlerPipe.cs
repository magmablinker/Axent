using System.Diagnostics;
using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.Core.Pipes.Observability;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Axent.Core.UnitTests.Pipes.Observability;

public sealed class TracingHandlerPipe : TestBase
{
    private static readonly ActivitySource _activitySource = new(ActivityTags.ActivityId);

    private readonly Mock<IActivityFactory> _activityFactoryMock = new();

    public TracingHandlerPipe()
    {
        _activityFactoryMock.Setup(m => m.Create<It.IsAnyType>())
            .Returns(() => _activitySource.StartActivity(string.Empty));
    }

    protected override void ConfigureAxent(AxentBuilder builder)
    {
        builder.AddTracing();
        builder.Services.AddSingleton<IActivityFactory>(_ => _activityFactoryMock.Object);
    }

    [Fact]
    public async Task SendAsync_start_activity_for_command()
    {
        // Arrange
        var command = new TestCommand("Hello World!");
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(command);

        // Assert
        Assert.True(response.IsSuccess);
        _activityFactoryMock.Verify(m => m.Create<TestCommand>(), Times.Once());
    }

    [Fact]
    public async Task SendAsync_start_activity_for_query()
    {
        // Arrange
        var query = new TestQuery();
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        Assert.True(response.IsSuccess);
        _activityFactoryMock.Verify(m => m.Create<TestQuery>(), Times.Once());
    }
}
