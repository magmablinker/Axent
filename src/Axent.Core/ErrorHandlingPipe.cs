using Axent.Abstractions;
using Microsoft.Extensions.Logging;

namespace Axent.Core;

public sealed class ErrorHandlingPipe<TRequest, TResponse>
    : IAxentPipe<TRequest, TResponse>
{
    private readonly AxentErrorHandlingOptions _options;
    private readonly ILogger<ErrorHandlingPipe<TRequest, TResponse>> _logger;

    public ErrorHandlingPipe(AxentOptions options, ILogger<ErrorHandlingPipe<TRequest, TResponse>> logger)
    {
        _options = options.ErrorHandling ?? new AxentErrorHandlingOptions();
        _logger = logger;
    }

    public async Task<Response<TResponse>> ProcessAsync(
        IPipelineChain<TRequest, TResponse> chain,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Call the next pipe in the chain
            return await chain.NextAsync(context, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unhandled exception occurred in the pipeline.");

            var response = ErrorDefaults.Generic.InternalServerError();
            if (_options.EnableDetailedExceptionResponse)
            {
                response.AddMessages(new[] { e.Message, e.StackTrace ?? "StackTrace is empty" });
            }

            return Response.Failure<TResponse>(response);
        }
    }
}
