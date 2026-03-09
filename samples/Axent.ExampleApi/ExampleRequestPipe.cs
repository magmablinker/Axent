using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;

namespace Axent.ExampleApi;

internal sealed class ExampleRequestPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly ILogger<ExampleRequestPipe<TRequest, TResponse>> _logger;

    public ExampleRequestPipe(ILogger<ExampleRequestPipe<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("This pipe runs during every request.");
        return chain.NextAsync(context, cancellationToken);
    }
}
