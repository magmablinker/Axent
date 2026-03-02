namespace Axent.Abstractions;

public interface IPipelineChain<TRequest, TResponse>
{
    /// <summary>
    /// Moves to the next pipe in the chain.
    /// </summary>
    Task<Response<TResponse>> NextAsync(
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default);
}
