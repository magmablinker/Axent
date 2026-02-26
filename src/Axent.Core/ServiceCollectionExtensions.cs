using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Axent.Abstractions;

namespace Axent.Core;

public static class ServiceCollectionExtensions
{
    public static AxentBuilder AddAxent(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>()
            .AddScoped<IRequestContextFactory, RequestContextFactory>()
            .AddScoped<IPipelineExecutorService, PipelineExecutorService>()
            .AddScoped(typeof(IHandlerPipe<,>), typeof(HandlerPipe<,>));
        return new(services);
    }
    
    public static AxentBuilder AddRequestHandlers(this AxentBuilder builder, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new
                {
                    Implementation = t,
                    Interface = i
                }))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            builder.Services.AddScoped(handler.Interface, handler.Implementation);
        }

        return builder;
    }

    public static AxentBuilder AddPipe<TPipe>(this AxentBuilder builder) where TPipe : IAxentPipe
    {
        var pipeType = typeof(TPipe);
        return builder.AddPipe(pipeType);
    }

    public static AxentBuilder AddPipe(this AxentBuilder builder, Type pipeType)
    {
        if (pipeType.IsGenericTypeDefinition)
        {
            builder.Services.AddScoped(typeof(IAxentPipe<,>), pipeType);
            return builder;
        }

        var serviceType = pipeType
                              .GetInterfaces()
                              .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAxentPipe<,>))
                          ?? throw new InvalidOperationException($"{pipeType.Name} does not implement IAxentPipe<,>");

        builder.Services.AddScoped(serviceType, pipeType);
        return builder;
    }
}