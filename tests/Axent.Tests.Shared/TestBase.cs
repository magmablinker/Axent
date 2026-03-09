using Axent.Abstractions.Builders;
using Axent.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Tests.Shared;

public abstract class TestBase
{
    private readonly Lazy<IServiceProvider> _serviceProvider;

    protected IServiceProvider ServiceProvider => _serviceProvider.Value;

    protected TestBase()
    {
        _serviceProvider = new Lazy<IServiceProvider>(() =>
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var axentBuilder = services.AddAxent(ConfigureAxentOptions)
                .AddHandlersFromAssemblyContaining<TestQueryHandler>();

            ConfigureAxent(axentBuilder);

            return services.BuildServiceProvider();
        });
    }

    protected virtual void ConfigureAxentOptions(AxentOptions options)
    {
        // empty on purpose
    }

    protected virtual void ConfigureAxent(IAxentBuilder builder)
    {
        // empty on purpose
    }
}
