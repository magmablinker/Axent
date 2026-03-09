using Axent.Abstractions.Models;

namespace Axent.Abstractions.Factories;

public interface IRequestContextFactory
{
    RequestContext<TRequest> Get<TRequest>(TRequest request);
}
