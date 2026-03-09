using System.Transactions;
using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.Core.Pipes.Transactions;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Axent.Core.UnitTests.Pipes.Transactions;

public sealed class TransactionalHandlerTest : TestBase
{
    private readonly Mock<ITransactionScopeFactory> _transactionScopeFactoryMock = new();

    public TransactionalHandlerTest()
    {
        _transactionScopeFactoryMock.Setup(m => m.Create())
            .Returns(new TransactionScope());
    }

    protected override void ConfigureAxentOptions(AxentOptions options)
    {
        options.Transactions.UseTransactions = true;
    }

    protected override void ConfigureAxent(AxentBuilder builder)
    {
        builder.Services.AddSingleton<ITransactionScopeFactory>(_ => _transactionScopeFactoryMock.Object);
    }

    [Fact]
    public async Task SendAsync_should_use_transaction_for_command()
    {
        // Arrange
        var command = new TestCommand("Hello World!");
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(command);

        // Assert
        Assert.True(response.IsSuccess);
        _transactionScopeFactoryMock.Verify(m => m.Create(), Times.Once());
    }

    [Fact]
    public async Task SendAsync_should_not_use_transaction_for_query()
    {
        // Arrange
        var query = new TestQuery();
        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        Assert.True(response.IsSuccess);
        _transactionScopeFactoryMock.Verify(m => m.Create(), Times.Never());
    }
}
