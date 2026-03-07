using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.DependencyInjection;

public sealed class AxentBuilder
{
    public IServiceCollection Services { get; }

    public AxentBuilder(IServiceCollection serviceCollection)
    {
        Services = serviceCollection;
    }
}
