using Axent.Abstractions.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.Caching;

internal sealed class AxentCacheBuilder : IAxentCacheBuilder
{
    public IServiceCollection Services { get; }

    public AxentCacheBuilder(IAxentBuilder builder)
    {
        Services = builder.Services;
    }
}
