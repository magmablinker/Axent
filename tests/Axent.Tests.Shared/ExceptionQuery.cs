using Axent.Abstractions.Attributes;
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Tests.Shared;

[Axent]
public sealed record ExceptionQuery(bool ThrowException) : IQuery<Unit>;

internal sealed class ExceptionQueryHandler : IRequestHandler<ExceptionQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(ExceptionQuery request, CancellationToken cancellationToken = default)
    {
        return request.ThrowException ? throw new InvalidOperationException() : ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
