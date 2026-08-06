using Axent.Abstractions.Attributes;
using Axent.Abstractions.Caching;
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;

namespace Axent.Tests.Shared;

[Axent]
[Authorize]
public sealed record ProtectedCacheQuery : ICacheableQuery<Unit>
{
    public string CacheKey => nameof(ProtectedCacheQuery);
    public bool BypassCache => false;

    // Explicit global scope acknowledges that this test response may be shared.
    public CacheScope CacheScope => CacheScope.Global;
}

internal sealed class ProtectedCacheQueryHandler : IRequestHandler<ProtectedCacheQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(
        ProtectedCacheQuery request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success(Unit.Value));
}
