namespace Axent.Abstractions;

public interface IRequestContextFactory
{
    RequestContext<TRequest> Get<TRequest>(TRequest request);
}
