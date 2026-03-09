namespace Axent.Abstractions.Models;

public sealed class RequestContext<TRequest>
{
    public required TRequest Request { get; init; }
}
