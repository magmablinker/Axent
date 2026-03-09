using Axent.Core.DependencyInjection;
using Axent.Core.Factories;
using Axent.Core.Pipes.Observability;
using Axent.Core.Pipes.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Core.UnitTests.DependencyInjection;

public sealed class AxentBuilderExtensionsTest
{
    [Fact]
    public void AddAxent_should_add_required_services()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAxent(o =>
        {
            o.Transactions.UseTransactions = true;
            o.Logging.EnableRequestLogging = true;
            o.ErrorHandling = new AxentErrorHandlingOptions();
        });

        // Assert
        Assert.Contains(serviceCollection, s =>
            s.ImplementationInstance?.GetType() == typeof(AxentOptions) && s.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(serviceCollection, s =>
            s.ImplementationType == typeof(RequestContextFactory) && s.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(serviceCollection, s =>
            s.ImplementationType == typeof(TransactionScopeFactory) && s.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(serviceCollection, s =>
            s.ImplementationType == typeof(TransactionPipe<,>) && s.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(serviceCollection, s =>
            s.ImplementationType == typeof(ErrorHandlingPipe<,>) && s.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(serviceCollection, s =>
            s.ImplementationType == typeof(RequestLoggerPipe<,>) && s.Lifetime == ServiceLifetime.Scoped);
    }
}
