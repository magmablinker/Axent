using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Axent.Abstractions;

namespace Axent.Core;

#pragma warning disable S2326
// ReSharper disable once UnusedTypeParameter
internal interface IHandlerPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse> where TRequest : notnull
#pragma warning restore S2326
{
}

internal sealed class HandlerPipe<TRequest, TResponse> : IHandlerPipe<TRequest, TResponse> where TRequest : class, IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HandlerPipe<TRequest, TResponse>> _logger;

    public HandlerPipe(IServiceProvider serviceProvider, ILogger<HandlerPipe<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(Func<ValueTask<Response<TResponse>>> next,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var handler = _serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
            if (handler is null)
                throw new InvalidOperationException($"No handler found for request type {context.Request.GetType()}");
            return await handler.HandleAsync(context, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured while processing the request");
            return Response.Failure<TResponse>(ErrorDefaults.Generic.InternalServerError());
        }
    }
}