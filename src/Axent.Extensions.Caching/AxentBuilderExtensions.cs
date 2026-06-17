using Axent.Abstractions.Builders;
using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Axent.Extensions.Caching;

public static class AxentBuilderExtensions
{
    public static IAxentBuilder AddCache(this IAxentBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.TryAddSingleton<ICache, InMemoryCache>();
        builder.Services.AddScoped(typeof(ICachePipe<,>), typeof(CachePipe<,>));
        return builder;
    }
}
