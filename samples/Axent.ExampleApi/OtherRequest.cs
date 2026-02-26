using Axent.Abstractions;
using Axent.Core;

namespace Axent.ExampleApi;

internal sealed class OtherRequest : IRequest<Unit>
{
    public required string Message { get; init; }
}

internal sealed class OtherRequestPipe : IAxentPipe<OtherRequest, Unit>
{
    private readonly ILogger<OtherRequestPipe> _logger;

    public OtherRequestPipe(ILogger<OtherRequestPipe> logger)
    {
        _logger = logger;
    }


    public ValueTask<Response<Unit>> ProcessAsync(Func<ValueTask<Response<Unit>>> next, RequestContext<OtherRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("I only run during other request");
        return next();
    }
}

internal sealed class OtherRequestHandler : RequestHandler<OtherRequest, Unit>
{
    private readonly ILogger<OtherRequestHandler> _logger;

    public OtherRequestHandler(ILogger<OtherRequestHandler> logger)
    {
        _logger = logger;
    }

    public override Task<Response<Unit>> HandleAsync(RequestContext<OtherRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{0}'", context.Request.Message);
        return Task.FromResult(Response.Success(Unit.Value));
    }
}