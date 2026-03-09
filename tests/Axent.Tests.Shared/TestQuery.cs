using Axent.Abstractions;

namespace Axent.Tests.Shared;

public sealed record TestQuery : IQuery<Unit>;

internal sealed class TestQueryHandler : IRequestHandler<TestQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(RequestContext<TestQuery> context, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
