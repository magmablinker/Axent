using System.Collections.Concurrent;
using System.Reflection;
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.Services;

internal sealed class Sender : ISender
{
    private static readonly MethodInfo _sendCoreMethod =
#pragma warning disable S3011
        typeof(Sender).GetMethod(nameof(SendCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)
#pragma warning restore S3011
        ?? throw new InvalidOperationException("Could not locate Axent sender dispatch method.");

    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ValueTask<Response<TResponse>> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var invoker = SenderInvokerCache<TResponse>.Get(request.GetType());
        return invoker(_serviceProvider, request, cancellationToken);
    }

    private static ValueTask<Response<TResponse>> SendCoreAsync<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var sender = serviceProvider.GetService<IRequestSender<TRequest, TResponse>>();
        return sender?.SendAsync((TRequest)request, cancellationToken) ?? throw new InvalidOperationException(
            $"No pipeline registered for request type '{typeof(TRequest).FullName}'.");
    }

    private delegate ValueTask<Response<TResponse>> SenderInvoker<TResponse>(
        IServiceProvider serviceProvider,
        IRequest<TResponse> request,
        CancellationToken cancellationToken);

    private static class SenderInvokerCache<TResponse>
    {
        private static readonly ConcurrentDictionary<Type, SenderInvoker<TResponse>> _invokers = new();

        public static SenderInvoker<TResponse> Get(Type requestType)
        {
            return _invokers.GetOrAdd(requestType, static type =>
            {
                var method = _sendCoreMethod.MakeGenericMethod(type, typeof(TResponse));
                return method.CreateDelegate<SenderInvoker<TResponse>>();
            });
        }
    }
}
