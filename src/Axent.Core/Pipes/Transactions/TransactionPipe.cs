using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;

namespace Axent.Core.Pipes.Transactions;

internal sealed class TransactionPipe<TRequest, TResponse> : ITransactionPipe<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly ITransactionScopeFactory _factory;

    public TransactionPipe(ITransactionScopeFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        using var scope = _factory.Create();

        var response = await chain.NextAsync(context, cancellationToken);
        scope.Complete();

        return response;
    }
}
