using System.Reflection;
using Axent.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core;

internal sealed class ReflectionSender : ISender
{
    private readonly IRequestContextFactory _requestContextFactory;
    private readonly IServiceProvider _serviceProvider;

    public ReflectionSender(IServiceProvider serviceProvider, IRequestContextFactory requestContextFactory)
    {
        _serviceProvider = serviceProvider;
        _requestContextFactory = requestContextFactory;
    }

    public async Task<Response<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var method = GetType()
            .GetMethod(nameof(SendInternalAsync), BindingFlags.Instance | BindingFlags.NonPublic)
            ?.MakeGenericMethod(requestType, typeof(TResponse));

        return await (Task<Response<TResponse>>)method?.Invoke(this, [request, cancellationToken])!;
    }

    private async Task<Response<TResponse>> SendInternalAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class, IRequest<TResponse>
    {
        var executor = _serviceProvider.GetRequiredService<IPipelineExecutorService>();
        var pipes = _serviceProvider.GetServices<IAxentPipe<TRequest, TResponse>>().ToArray();
        var handlerPipe = _serviceProvider.GetRequiredService<IHandlerPipe<TRequest, TResponse>>();
        var context = _requestContextFactory.Get(request);

        return await executor.ExecuteAsync([.. pipes, handlerPipe], context, cancellationToken);
    }
}
