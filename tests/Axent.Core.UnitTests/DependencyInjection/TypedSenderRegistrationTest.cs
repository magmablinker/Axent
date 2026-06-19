using Axent.Abstractions.Models;
using Axent.Abstractions.Services;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Core.UnitTests.DependencyInjection;

public sealed class TypedSenderRegistrationTest : TestBase
{
    [Fact]
    public async Task AddAxent_should_register_generated_typed_sender()
    {
        // Arrange
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<IRequestSender<TestQuery, Unit>>();

        // Act
        var response = await sender.SendAsync(new TestQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.IsSuccess);
    }
}
