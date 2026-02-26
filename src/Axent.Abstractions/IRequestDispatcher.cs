namespace Axent.Abstractions;

public interface IRequestDispatcher
{
    Task<Response<TResponse>> DispatchAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}