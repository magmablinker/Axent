using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Core.Services;

internal sealed class Sender : ISender
{
    private readonly IReadOnlyDictionary<Type, IRequestRoute> _routes;

    public Sender(IEnumerable<IAxentRequestModule> modules)
    {
        var routes = new Dictionary<Type, IRequestRoute>();
        var builder = new RequestRouteBuilder(routes);

        foreach (var module in modules)
        {
            module.RegisterRoutes(builder);
        }

        _routes = routes;
    }

    public ValueTask<Response<TResponse>> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        if (!_routes.TryGetValue(requestType, out var route))
        {
            throw new InvalidOperationException(
                $"No pipeline registered for request type '{requestType.FullName}'.");
        }

        return route.SendAsync(request, cancellationToken);
    }

    private interface IRequestRoute
    {
        ValueTask<Response<TResponse>> SendAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken);
    }

    private sealed class RequestRoute<TRequest, TResponse> : IRequestRoute
        where TRequest : IRequest<TResponse>
    {
        private readonly Func<TRequest, CancellationToken, ValueTask<Response<TResponse>>> _handler;

        public RequestRoute(
            Func<TRequest, CancellationToken, ValueTask<Response<TResponse>>> handler)
        {
            _handler = handler;
        }

        public async ValueTask<Response<TActualResponse>> SendAsync<TActualResponse>(
            IRequest<TActualResponse> request,
            CancellationToken cancellationToken)
        {
            var response = await _handler((TRequest)request, cancellationToken);
            return (Response<TActualResponse>)(object)response;
        }
    }

    private sealed class RequestRouteBuilder : IAxentRequestRouteBuilder
    {
        private readonly Dictionary<Type, IRequestRoute> _routes;

        public RequestRouteBuilder(Dictionary<Type, IRequestRoute> routes)
        {
            _routes = routes;
        }

        public void Map<TRequest, TResponse>(
            Func<TRequest, CancellationToken, ValueTask<Response<TResponse>>> handler)
            where TRequest : IRequest<TResponse>
        {
            var requestType = typeof(TRequest);

            if (!_routes.TryAdd(requestType, new RequestRoute<TRequest, TResponse>(handler)))
            {
                throw new InvalidOperationException(
                    $"More than one Axent route was registered for request type '{requestType.FullName}'.");
            }
        }
    }
}
