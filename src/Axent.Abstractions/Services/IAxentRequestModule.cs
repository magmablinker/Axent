using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;

namespace Axent.Abstractions.Services;

public interface IAxentRequestModule
{
    void RegisterRoutes(IAxentRequestRouteBuilder builder);
}

public interface IAxentRequestRouteBuilder
{
    void Map<TRequest, TResponse>(
        Func<TRequest, CancellationToken, ValueTask<Response<TResponse>>> handler)
        where TRequest : IRequest<TResponse>;
}
