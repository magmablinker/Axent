namespace Axent.Abstractions;

public sealed class RequestContext<TRequest>
{
    public required TRequest Request { get; init; }
}
