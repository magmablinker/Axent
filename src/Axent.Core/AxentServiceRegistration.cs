using Axent.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Axent.Core;

public static class AxentServiceRegistration
{
    public static AxentBuilder AddAxent(this IServiceCollection services, Action<AxentOptions>? configure = null)
    {
        var builder = new AxentBuilder(services);

        var options = new AxentOptions();
        configure?.Invoke(options);

        builder.Services
            .AddScoped<IRequestContextFactory, RequestContextFactory>()
            .AddScoped<IPipelineExecutorService, PipelineExecutorService>()
            .AddScoped(typeof(IHandlerPipe<,>), typeof(HandlerPipe<,>));

        if (options.UseSourceGeneratedSender)
        {
            AxentSenderRegistry.Apply(services);
        }
        else
        {
            services.AddScoped<ISender, ReflectionSender>();
        }

        return builder;
    }

    public static AxentBuilder AddRequestHandlers(
        this AxentBuilder builder,
        Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new { Implementation = t, Interface = i }))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            builder.Services.AddScoped(handler.Interface, handler.Implementation);
        }

        return builder;
    }

    public static AxentBuilder AddHandler<THandler>(this AxentBuilder builder)
        where THandler : class, IRequestHandler
    {
        var handlerType = typeof(THandler);

        var serviceType = handlerType
                              .GetInterfaces()
                              .FirstOrDefault(i =>
                                  i.IsGenericType &&
                                  i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                          ?? throw new AxentConfigurationException(
                              $"'{handlerType.Name}' does not implement IRequestHandler<TRequest, TResponse>.");

        builder.Services.AddScoped(serviceType, handlerType);

        return builder;
    }

    public static AxentBuilder AddPipe<TPipe>(this AxentBuilder builder)
        where TPipe : IAxentPipe
        => builder.AddPipe(typeof(TPipe));

    public static AxentBuilder AddPipe(this AxentBuilder builder, Type pipeType)
    {
        if (pipeType.IsGenericTypeDefinition)
        {
            builder.Services.AddScoped(typeof(IAxentPipe<,>), pipeType);
            return builder;
        }

        var serviceType =
            pipeType
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IAxentPipe<,>))
            ?? throw new InvalidOperationException(
                $"{pipeType.Name} does not implement IAxentPipe<,>");

        builder.Services.AddScoped(serviceType, pipeType);
        return builder;
    }
}
