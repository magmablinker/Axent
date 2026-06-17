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

    public async ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        using var scope = _factory.Create();

        var response = await next(request, cancellationToken);
        if (response.IsSuccess)
        {
            scope.Complete();
        }

        return response;
    }
}
