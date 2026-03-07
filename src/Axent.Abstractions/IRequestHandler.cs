namespace Axent.Abstractions;

/// <summary>
/// Marker interface. Do not implement.
/// </summary>
public interface IRequestHandler;

public interface IRequestHandler<TRequest, TResponse> : IRequestHandler
    where TRequest : class, IRequest<TResponse>
{
    ValueTask<Response<TResponse>> HandleAsync(RequestContext<TRequest> context, CancellationToken cancellationToken = default);
}
