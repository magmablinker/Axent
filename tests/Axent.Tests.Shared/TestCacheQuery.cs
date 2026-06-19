using Axent.Abstractions.Attributes;
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Tests.Shared;

[Axent]
public sealed record TestCacheQuery(string Message, bool BypassCache = false) : ICacheableQuery<string>
{
    public string CacheKey => nameof(TestCacheQuery);
}

internal sealed class TestCacheQueryHandler : IRequestHandler<TestCacheQuery, string>
{
    public ValueTask<Response<string>> HandleAsync(TestCacheQuery request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success(request.Message));
}
