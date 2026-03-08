using Axent.Abstractions;

namespace Axent.ExampleApi;

internal sealed class ExampleCommand : ICommand<ExampleResponse>
{
    public required string Message { get; init; }
}

internal sealed class ExampleResponse
{
    public required string Message { get; init; }
}

internal sealed class ExampleRequestHandler : IRequestHandler<ExampleCommand, ExampleResponse>
{
    private static readonly Random Random = new();

    private readonly ILogger<ExampleRequestHandler> _logger;

    public ExampleRequestHandler(ILogger<ExampleRequestHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<ExampleResponse>> HandleAsync(RequestContext<ExampleCommand> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{Message}'", context.Request.Message);
        return ValueTask.FromResult(Random.Next(1, 100) % 2 == 0
            ? throw new InvalidOperationException()
            : Response.Success(new ExampleResponse { Message = context.Request.Message }));
    }
}
