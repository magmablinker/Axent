using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.Caching;

public interface IAxentCacheBuilder
{
    IServiceCollection Services { get; }
}
