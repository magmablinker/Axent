using System.Transactions;
using Axent.Abstractions.Builders;
using Axent.Abstractions.Services;
using Axent.Core.DependencyInjection;
using Axent.Core.Pipes.Transactions;
using Axent.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Axent.Core.UnitTests.Pipes.Transactions;

public sealed class TransactionalHandlerTest : TestBase
{
    private readonly ITransactionScopeFactory _transactionScopeFactory = Substitute.For<ITransactionScopeFactory>();

    public TransactionalHandlerTest()
    {
        _transactionScopeFactory.Create()
            .Returns(_ => new TransactionScope());
    }

    protected override void ConfigureAxentOptions(AxentOptions options)
    {
        options.Transactions.UseTransactions = true;
    }

    protected override void ConfigureAxent(IAxentBuilder builder)
    {
        builder.Services.AddSingleton(_transactionScopeFactory);
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
        _transactionScopeFactory.Received(1).Create();
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
        _transactionScopeFactory.DidNotReceive().Create();
    }
}
