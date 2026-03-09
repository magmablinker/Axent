using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;

namespace Axent.Core.Pipes.Transactions;

public interface ITransactionPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
    where TRequest : ICommand<TResponse>;
