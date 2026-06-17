using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Core.Services;

internal sealed class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<Type, IRequestRoute> _routes;

    public Sender(
        IServiceProvider serviceProvider,
        IEnumerable<IAxentRequestModule> modules)
    {
        _serviceProvider = serviceProvider;

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

        return route.SendAsync(_serviceProvider, request, cancellationToken);
    }

    private interface IRequestRoute
    {
        ValueTask<Response<TResponse>> SendAsync<TResponse>(
            IServiceProvider serviceProvider,
            IRequest<TResponse> request,
            CancellationToken cancellationToken);
    }

    private sealed class RequestRoute<TRequest, TResponse> : IRequestRoute
        where TRequest : IRequest<TResponse>
    {
        private readonly AxentRequestExecutor<TRequest, TResponse> _executor;

        public RequestRoute(AxentRequestExecutor<TRequest, TResponse> executor)
        {
            _executor = executor;
        }

        public async ValueTask<Response<TActualResponse>> SendAsync<TActualResponse>(
            IServiceProvider serviceProvider,
            IRequest<TActualResponse> request,
            CancellationToken cancellationToken)
        {
            var response = await _executor(
                serviceProvider,
                (TRequest)request,
                cancellationToken);

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
            AxentRequestExecutor<TRequest, TResponse> executor) where TRequest : IRequest<TResponse>
        {
            var requestType = typeof(TRequest);

            if (!_routes.TryAdd(requestType, new RequestRoute<TRequest, TResponse>(executor)))
            {
                throw new InvalidOperationException(
                    $"More than one Axent route was registered for request type '{requestType.FullName}'.");
            }
        }
    }
}
