using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;

namespace Axent.Abstractions.Services;

public interface IAxentRequestModule
{
    void RegisterRoutes(IAxentRequestRouteBuilder builder);
}

public delegate ValueTask<Response<TResponse>> AxentRequestExecutor<in TRequest, TResponse>(
    IServiceProvider serviceProvider,
    TRequest request,
    CancellationToken cancellationToken);

public interface IAxentRequestRouteBuilder
{
    void Map<TRequest, TResponse>(
        AxentRequestExecutor<TRequest, TResponse> executor) where TRequest : IRequest<TResponse>;
}
