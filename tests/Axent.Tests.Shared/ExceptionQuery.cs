using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Tests.Shared;

public sealed record ExceptionQuery(bool ThrowException) : IQuery<Unit>;

internal sealed class ExceptionQueryHandler : IRequestHandler<ExceptionQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(RequestContext<ExceptionQuery> context, CancellationToken cancellationToken = default)
    {
        return context.Request.ThrowException ? throw new InvalidOperationException() : ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
