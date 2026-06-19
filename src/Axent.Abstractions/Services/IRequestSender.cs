using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;

namespace Axent.Abstractions.Services;

public interface IRequestSender<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<Response<TResponse>> SendAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
