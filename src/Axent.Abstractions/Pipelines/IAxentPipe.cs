using Axent.Abstractions.Models;

namespace Axent.Abstractions.Pipelines;

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
    /// <param name="chain">Internal pipeline chain providing NextAsync.</param>
    /// <param name="context">Request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response from this pipe or downstream.</returns>
    ValueTask<Response<TResponse>> ProcessAsync(
        IPipelineChain<TRequest, TResponse> chain,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default);
}
