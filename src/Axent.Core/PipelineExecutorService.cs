using Axent.Abstractions;

namespace Axent.Core;

internal sealed class PipelineExecutorService : IPipelineExecutorService
{
    public async ValueTask<Response<TResponse>> ExecuteAsync<TRequest, TResponse>(IEnumerable<IAxentPipe<TRequest, TResponse>> pipes,
        RequestContext<TRequest> request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var queue = new Queue<IAxentPipe<TRequest, TResponse>>(pipes);
        queue.Enqueue(new TerminationPipe<TRequest, TResponse>());
        return await Dequeue(queue, request, cancellationToken);
    }

    private static async ValueTask<Response<TResponse>> Dequeue<TRequest, TResponse>(Queue<IAxentPipe<TRequest, TResponse>> queue,
        RequestContext<TRequest> request,
        CancellationToken cancellationToken = default) =>
        await queue.Dequeue()
            .ProcessAsync(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await Dequeue(queue, request, cancellationToken);
                },
                request,
                cancellationToken);
}