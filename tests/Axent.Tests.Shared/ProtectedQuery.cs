using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;

namespace Axent.Tests.Shared;

[Authorize]
public sealed record ProtectedQuery : IQuery<Unit>;

internal sealed class ProtectedQueryHandler : IRequestHandler<ProtectedQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(RequestContext<ProtectedQuery> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success(Unit.Value));
}
