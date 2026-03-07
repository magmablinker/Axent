namespace Axent.Abstractions;

public interface ISender
{
    ValueTask<Response<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
