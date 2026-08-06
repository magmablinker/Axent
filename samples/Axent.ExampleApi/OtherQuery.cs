using Axent.Abstractions.Attributes;
using Axent.Abstractions.Caching;
using Axent.Abstractions.Models;
using Axent.Abstractions.Options;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.ExampleApi;

[Axent]
internal sealed class OtherQuery : ICacheableQuery<OtherResponse>
{
    public required string Message { get; init; }
    public string CacheKey => $"{nameof(OtherQuery)}-{Message}";
    public bool BypassCache => false;
    public CacheScope CacheScope => CacheScope.Culture;
    public CacheEntryOptions CacheOptions => new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        Tags = ["other-query"],
    };
}

internal sealed class OtherResponse
{
    public required string Message { get; init; }
}

internal sealed class OtherQueryPipe : IAxentPipe<OtherQuery, OtherResponse>
{
    private readonly ILogger<OtherQueryPipe> _logger;

    public OtherQueryPipe(ILogger<OtherQueryPipe> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<OtherResponse>> ProcessAsync(
        OtherQuery request,
        AxentPipelineContinuation<OtherQuery, OtherResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("I only run during other request");
        return next(request, cancellationToken);
    }
}

internal sealed class OtherQueryHandler : IRequestHandler<OtherQuery, OtherResponse>
{
    private readonly ILogger<OtherQueryHandler> _logger;

    public OtherQueryHandler(ILogger<OtherQueryHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<OtherResponse>> HandleAsync(OtherQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{Message}'", request.Message);
        return ValueTask.FromResult(Response.Success(new OtherResponse { Message = request.Message }));
    }
}
