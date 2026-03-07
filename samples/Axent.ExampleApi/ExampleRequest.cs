using Axent.Abstractions;
using Axent.Core;

namespace Axent.ExampleApi;

internal sealed class ExampleRequest : IRequest<ExampleResponse>
{
    public required string Message { get; init; }
}

internal sealed class ExampleResponse
{
    public required string Message { get; init; }
}

internal sealed class ExampleRequestHandler : IRequestHandler<ExampleRequest, ExampleResponse>
{
    private static readonly Random Random = new ();

    private readonly ILogger<ExampleRequestHandler> _logger;

    public ExampleRequestHandler(ILogger<ExampleRequestHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<ExampleResponse>> HandleAsync(RequestContext<ExampleRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{0}'", context.Request.Message);
        return ValueTask.FromResult(Random.Next(1, 100) % 2 == 0
            ? Response.Failure(ErrorDefaults.Generic.BadRequest())
            : Response.Success(new ExampleResponse { Message = context.Request.Message }));
    }
}
