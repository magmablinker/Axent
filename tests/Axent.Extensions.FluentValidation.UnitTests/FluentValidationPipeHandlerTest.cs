using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.Tests.Shared;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Extensions.FluentValidation.UnitTests;

public sealed class FluentValidationPipeHandlerTest : TestBase
{
    protected override void ConfigureAxent(AxentBuilder builder)
    {
        builder.AddAutoFluentValidation();
        builder.Services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
    }

    [Fact]
    public async Task SendAsync_should_validate_command_and_fail()
    {
        // Arrange
        var command = new TestCommand(string.Join(string.Empty, Enumerable.Repeat("a", 21)));
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<TestCommand>>();
        var validationResult = await validator.ValidateAsync(command);
        var errorFactory = scope.ServiceProvider.GetRequiredService<IFluentValidationErrorFactory>();

        // Act
        var response = await sender.SendAsync(command);

        // Assert
        Assert.True(response.IsFailure);
        Assert.Equal(response.Error.Identifier, ErrorDefaults.Generic.ValidationFailure().Identifier);
        Assert.Equivalent(errorFactory.Create<Unit>(validationResult.Errors), response);
    }

    [Fact]
    public async Task SendAsync_should_validate_command_and_succeed()
    {
        // Arrange
        var command = new TestCommand("Im Valid");
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(command);

        // Assert
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task SendAsync_should_not_validate_query_without_validator()
    {
        // Arrange
        var query = new TestQuery();
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        Assert.True(response.IsSuccess);
    }
}
