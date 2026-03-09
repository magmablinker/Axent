using Axent.Abstractions;

namespace Axent.ExampleApi;

public sealed class ExampleCommand : ICommand<ExampleResponse>
{
    public required string Message { get; init; }
}

internal sealed class ExampleResponse
{
    public required string Message { get; init; }
}

internal sealed class ExampleCommandHandler : IRequestHandler<ExampleCommand, ExampleResponse>
{
    private static readonly Random Random = new();

    private readonly ILogger<ExampleCommandHandler> _logger;

    public ExampleCommandHandler(ILogger<ExampleCommandHandler> logger)
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
