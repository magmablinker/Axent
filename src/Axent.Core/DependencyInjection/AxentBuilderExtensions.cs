using System.Reflection;
using Axent.Abstractions.Builders;
using Axent.Abstractions.Factories;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Services;
using Axent.Core.Factories;
using Axent.Core.Pipes.Observability;
using Axent.Core.Pipes.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.DependencyInjection;

public static class AxentBuilderExtensions
{
    /// <summary>
    /// Add the services required by axent
    /// </summary>
    /// <param name="services">ServiceCollection</param>
    /// <param name="configure">Action to configure the <see cref="AxentOptions"/></param>
    /// <param name="assemblies">Assemblies which get scanned for source generated handlers</param>
    /// <returns></returns>
    public static IAxentBuilder AddAxent(this IServiceCollection services, Action<AxentOptions>? configure = null, Assembly[]? assemblies = null)
    {
        assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

        var builder = new AxentBuilder(services);

        var options = new AxentOptions();
        configure?.Invoke(options);

        builder.Services
            .AddSingleton(options)
            .AddScoped<IRequestContextFactory, RequestContextFactory>();

        if (options.Transactions.UseTransactions)
        {
            builder.AddTransactions();
        }

        if (options.ErrorHandling is not null)
        {
            builder.AddPipe(typeof(ErrorHandlingPipe<,>));
        }

        if (options.Logging.EnableRequestLogging)
        {
            builder.AddPipe(typeof(RequestLoggerPipe<,>));
        }

        builder.AddSourceGeneratedServices(assemblies);

        return builder;
    }

    public static IAxentBuilder AddRequestHandlersFromAssembly(
        this IAxentBuilder builder,
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

    public static IAxentBuilder AddHandlersFromAssemblyContaining<THandler>(this IAxentBuilder builder) where THandler : IRequestHandler
    {
        return builder.AddRequestHandlersFromAssembly(typeof(THandler).Assembly);
    }

    public static IAxentBuilder AddHandler<THandler>(this IAxentBuilder builder)
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

    public static IAxentBuilder AddTracing(this IAxentBuilder builder)
    {
        builder.Services.AddSingleton<IActivityFactory, ActivityFactory>();
        builder.AddPipe(typeof(TracingPipe<,>));
        return builder;
    }

    private static void AddTransactions(this IAxentBuilder builder)
    {
        builder.Services.AddSingleton<ITransactionScopeFactory, TransactionScopeFactory>();
        builder.Services.AddScoped(typeof(ITransactionPipe<,>), typeof(TransactionPipe<,>));
    }

    private static IAxentBuilder AddSourceGeneratedServices(
        this AxentBuilder builder,
        Assembly[] assemblies)
    {
        var senderType = typeof(ISender);
        var pipelineType = typeof(IPipeline);
        var handlerPipeType = typeof(IHandlerPipe);

        var allTypes = assemblies.SelectMany(a => a.GetTypes())
            .ToList();
        var sender = allTypes.First(t => t is { IsAbstract: false, IsInterface: false } && senderType.IsAssignableFrom(t));
        builder.Services.AddScoped(senderType, sender);

        foreach (var type in allTypes.Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            if (pipelineType.IsAssignableFrom(type))
            {
                builder.Services.AddScoped(type);
            }

            if (handlerPipeType.IsAssignableFrom(type))
            {
                builder.Services.AddScoped(type);
            }
        }

        return builder;
    }
}
