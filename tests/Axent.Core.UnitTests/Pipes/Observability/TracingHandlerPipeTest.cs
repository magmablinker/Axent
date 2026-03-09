using System.Diagnostics;
using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.Core.Pipes.Observability;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Axent.Core.UnitTests.Pipes.Observability;

public sealed class TracingHandlerPipeTest : TestBase
{
    private static readonly ActivitySource _activitySource = new(ActivityTags.ActivityId);

    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();

    public TracingHandlerPipeTest()
    {
        _activityFactory.Create<TestCommand>()
            .Returns(_ => _activitySource.StartActivity(string.Empty));

        _activityFactory.Create<TestQuery>()
            .Returns(_ => _activitySource.StartActivity(string.Empty));
    }

    protected override void ConfigureAxent(AxentBuilder builder)
    {
        builder.AddTracing();
        builder.Services.AddSingleton(_activityFactory);
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
        _activityFactory.Received(1).Create<TestCommand>();
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
        _activityFactory.Received(1).Create<TestQuery>();
    }
}
