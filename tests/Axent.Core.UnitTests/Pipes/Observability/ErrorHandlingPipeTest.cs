using Axent.Abstractions.Models;
using Axent.Abstractions.Services;
using Axent.Core.DependencyInjection;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Core.UnitTests.Pipes.Observability;

public sealed class ErrorHandlingPipeTest : TestBase
{
    protected override void ConfigureAxentOptions(AxentOptions options)
    {
        options.ErrorHandling = new AxentErrorHandlingOptions { EnableDetailedExceptionResponse = true };
    }

    [Fact]
    public async Task SendAsync_should_catch_exception_and_return_internal_server_error()
    {
        // Arrange
        var query = new ExceptionQuery(true);
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        var invalidOperationException = new InvalidOperationException();
        Assert.True(response.IsFailure);
        Assert.Equal(response.Error.Identifier, ErrorDefaults.Generic.InternalServerError().Identifier);
        Assert.Equal(response.Error.Messages[0], invalidOperationException.Message);
    }
}
