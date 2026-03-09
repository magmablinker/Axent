using System.Transactions;

namespace Axent.Core.Pipes.Transactions;

internal interface ITransactionScopeFactory
{
    TransactionScope Create();
}
