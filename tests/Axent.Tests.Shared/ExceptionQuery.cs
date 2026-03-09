using Axent.Abstractions;

namespace Axent.Tests.Shared;

public sealed record ExceptionQuery(bool ThrowException) : IQuery<Unit>;

internal sealed class ExceptionQueryHandler : IRequestHandler<ExceptionQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(RequestContext<ExceptionQuery> context, CancellationToken cancellationToken = default)
    {
        return context.Request.ThrowException ? throw new InvalidOperationException() : ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
