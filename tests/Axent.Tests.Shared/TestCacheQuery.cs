using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Tests.Shared;

public sealed record TestCacheQuery(string Message, bool BypassCache = false) : ICacheableQuery<string>
{
    public string CacheKey => nameof(TestCacheQuery);
}

internal sealed class TestCacheQueryHandler : IRequestHandler<TestCacheQuery, string>
{
    public ValueTask<Response<string>> HandleAsync(RequestContext<TestCacheQuery> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success(context.Request.Message));
}
