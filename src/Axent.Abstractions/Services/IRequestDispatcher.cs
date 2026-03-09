using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;

namespace Axent.Abstractions.Services;

public interface IRequestDispatcher
{
    Task<Response<TResponse>> DispatchAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
