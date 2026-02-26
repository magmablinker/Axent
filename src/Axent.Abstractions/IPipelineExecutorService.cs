namespace Axent.Abstractions;

public interface IPipelineExecutorService
{
    ValueTask<Response<TResponse>> ExecuteAsync<TRequest, TResponse>(IEnumerable<IAxentPipe<TRequest, TResponse>> pipes,
        RequestContext<TRequest> request,
        CancellationToken cancellationToken = default);
}
