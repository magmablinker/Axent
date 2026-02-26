namespace Axent.Abstractions;

/// <summary>
/// Marker interface, do not implement it.
/// </summary>
public interface IAxentPipe;

public interface IAxentPipe<TRequest, TResponse> : IAxentPipe
{
    ValueTask<Response<TResponse>> ProcessAsync(Func<ValueTask<Response<TResponse>>> next,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default);
}
