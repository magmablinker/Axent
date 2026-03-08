using System.Transactions;
using Axent.Abstractions;
using Axent.Core.DependencyInjection;

namespace Axent.Core.Pipes.Transactions;

internal sealed class TransactionPipe<TRequest, TResponse> : ITransactionPipe<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly AxentTransactionOptions _options;

    public TransactionPipe(AxentOptions options)
    {
        _options = options.Transactions;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(_options.TransactionScopeOption,
            _options.TransactionOptions,
            _options.TransactionScopeAsyncFlowOption);

        var response = await chain.NextAsync(context, cancellationToken);
        scope.Complete();

        return response;
    }
}
