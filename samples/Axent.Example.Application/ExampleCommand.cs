using Axent.Abstractions.Attributes;
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Axent.Example.Application;

[Axent]
public sealed class ExampleCommand : ICommand<ExampleResponse>
{
    public required string Message { get; init; }
}

public sealed class ExampleResponse
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

    public ValueTask<Response<ExampleResponse>> HandleAsync(ExampleCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{Message}'", request.Message);
        return ValueTask.FromResult(Random.Next(1, 100) % 2 == 0
            ? throw new InvalidOperationException()
            : Response.Success(new ExampleResponse { Message = request.Message }));
    }
}
