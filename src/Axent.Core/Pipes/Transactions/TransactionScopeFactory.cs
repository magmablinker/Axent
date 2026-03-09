using System.Transactions;
using Axent.Core.DependencyInjection;

namespace Axent.Core.Pipes.Transactions;

internal sealed class TransactionScopeFactory : ITransactionScopeFactory
{
    private readonly AxentTransactionOptions _options;

    public TransactionScopeFactory(AxentOptions options)
    {
        _options = options.Transactions;
    }

    public TransactionScope Create()
    {
        return new TransactionScope(_options.TransactionScopeOption,
            _options.TransactionOptions,
            _options.TransactionScopeAsyncFlowOption);
    }
}
