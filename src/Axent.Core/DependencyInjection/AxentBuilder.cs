using Axent.Abstractions.Builders;
using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.DependencyInjection;

internal sealed class AxentBuilder : IAxentBuilder
{
    public IServiceCollection Services { get; }

    public AxentBuilder(IServiceCollection serviceCollection)
    {
        Services = serviceCollection;
    }

    public IAxentBuilder AddPipe(Type pipeType)
    {
        if (pipeType.IsGenericTypeDefinition)
        {
            Services.AddScoped(typeof(IAxentPipe<,>), pipeType);
            return this;
        }

        var serviceType =
            pipeType
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IAxentPipe<,>))
            ?? throw new InvalidOperationException(
                $"{pipeType.Name} does not implement IAxentPipe<,>");

        Services.AddScoped(serviceType, pipeType);
        return this;
    }
}
