using Axent.Abstractions.Models;
using Axent.Abstractions.Options;
using Axent.Abstractions.Pipelines;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.ExampleApi;

internal sealed class OtherQuery : ICacheableQuery<OtherResponse>
{
    public required string Message { get; init; }
    public string CacheKey => $"{nameof(OtherQuery)}-{Message}";
    public bool BypassCache => false;
    public CacheEntryOptions CacheOptions => new() { SlidingExpiration = TimeSpan.FromMinutes(5) };
}

internal sealed class OtherResponse
{
    public required string Message { get; init; }
}

internal sealed class OtherRequestPipe : IAxentPipe<OtherQuery, OtherResponse>
{
    private readonly ILogger<OtherRequestPipe> _logger;

    public OtherRequestPipe(ILogger<OtherRequestPipe> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<OtherResponse>> ProcessAsync(IPipelineChain<OtherQuery, OtherResponse> chain, RequestContext<OtherQuery> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("I only run during other request");
        return chain.NextAsync(context, cancellationToken);
    }
}

internal sealed class OtherRequestHandler : IRequestHandler<OtherQuery, OtherResponse>
{
    private readonly ILogger<OtherRequestHandler> _logger;

    public OtherRequestHandler(ILogger<OtherRequestHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<OtherResponse>> HandleAsync(RequestContext<OtherQuery> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{0}'", context.Request.Message);
        return ValueTask.FromResult(Response.Success(new OtherResponse { Message = context.Request.Message }));
    }
}
