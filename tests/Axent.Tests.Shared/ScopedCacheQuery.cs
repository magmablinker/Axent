using Axent.Abstractions.Attributes;
using Axent.Abstractions.Caching;
using Axent.Abstractions.Models;
using Axent.Abstractions.Options;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Tests.Shared;

[Axent]
public sealed record ScopedCacheQuery(
    string Message,
    CacheScope Scope = CacheScope.User,
    string? Tag = null)
    : ICacheableQuery<string>
{
    public string CacheKey => nameof(ScopedCacheQuery);
    public bool BypassCache => false;
    public CacheScope CacheScope => Scope;
    public CacheEntryOptions CacheOptions => new() { Tags = Tag is null ? [] : [Tag] };
}

internal sealed class ScopedCacheQueryHandler : IRequestHandler<ScopedCacheQuery, string>
{
    public ValueTask<Response<string>> HandleAsync(
        ScopedCacheQuery request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success(request.Message));
}
