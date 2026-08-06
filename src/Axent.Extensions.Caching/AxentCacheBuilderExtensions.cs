using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.Caching;

public static class AxentCacheBuilderExtensions
{
    extension(IAxentCacheBuilder builder)
    {
        public IAxentCacheBuilder UseInMemory()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<ICache, InMemoryCache>();
            return builder;
        }
    }
}
