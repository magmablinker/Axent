namespace Axent.Abstractions;

public interface ISender
{
    Task<Response<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
