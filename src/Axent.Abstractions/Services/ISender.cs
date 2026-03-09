using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;

namespace Axent.Abstractions.Services;

public interface ISender
{
    ValueTask<Response<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
