using Axent.Abstractions.Models;

namespace Axent.Abstractions.Pipelines;

public delegate ValueTask<Response<TResponse>> AxentPipelineContinuation<TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken = default);

/// <summary>
/// Marker interface for Axent pipeline pipes.
/// Do not implement it
/// </summary>
public interface IAxentPipe { }

public interface IAxentPipe<TRequest, TResponse> : IAxentPipe
{
    /// <summary>
    /// Processes the request and optionally calls the next pipe in the pipeline.
    /// </summary>
    /// <param name="request">Request instance.</param>
    /// <param name="next">Generated continuation for the next pipeline step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response from this pipe or downstream.</returns>
    ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default);
}
