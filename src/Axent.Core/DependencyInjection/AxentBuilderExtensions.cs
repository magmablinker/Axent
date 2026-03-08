using System.Reflection;
using Axent.Abstractions;
using Axent.Core.Pipes.Observability;
using Axent.Core.Pipes.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.DependencyInjection;

public static class AxentBuilderExtensions
{
    public static AxentBuilder AddAxent(this IServiceCollection services, Action<AxentOptions>? configure = null)
    {
        var builder = new AxentBuilder(services);

        var options = new AxentOptions();
        configure?.Invoke(options);

        builder.Services
            .AddSingleton(options)
            .AddScoped<IRequestContextFactory, RequestContextFactory>();

        if (options.Transactions.UseTransactions)
        {
            builder.AddTransactionPipe();
        }

        if (options.ErrorHandling is not null)
        {
            builder.AddPipe(typeof(ErrorHandlingPipe<,>));
        }

        if (options.Logging.EnableRequestLogging)
        {
            builder.AddPipe(typeof(RequestLoggerPipe<,>));
        }

        AxentSenderRegistry.Apply(services);

        return builder;
    }

    public static AxentBuilder AddRequestHandlersFromAssembly(
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

    public static AxentBuilder AddTracing(this AxentBuilder builder)
    {
        builder.AddPipe(typeof(TracingPipe<,>));
        return builder;
    }

    private static void AddTransactionPipe(this AxentBuilder builder)
    {
        builder.Services.AddScoped(typeof(ITransactionPipe<,>), typeof(TransactionPipe<,>));
    }
}
