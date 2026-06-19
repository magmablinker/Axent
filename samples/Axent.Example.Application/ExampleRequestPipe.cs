using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.Logging;

namespace Axent.Example.Application;

public sealed class ExampleRequestPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly ILogger<ExampleRequestPipe<TRequest, TResponse>> _logger;

    public ExampleRequestPipe(ILogger<ExampleRequestPipe<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("This pipe runs during every request.");
        return next(request, cancellationToken);
    }
}
