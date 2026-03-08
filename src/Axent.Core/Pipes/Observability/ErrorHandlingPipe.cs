using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Axent.Core.Pipes.Observability;

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

    public async ValueTask<Response<TResponse>> ProcessAsync(
        IPipelineChain<TRequest, TResponse> chain,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await chain.NextAsync(context, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.PipelineExecutionFailed(e);

            var response = ErrorDefaults.Generic.InternalServerError();
            if (_options.EnableDetailedExceptionResponse)
            {
                response.AddMessages([e.Message, e.StackTrace ?? "StackTrace is empty"]);
            }

            return Response.Failure<TResponse>(response);
        }
    }
}

internal static partial class ErrorHandlingPipeLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred during the pipeline execution."),
        ]
    public static partial void PipelineExecutionFailed(
        this ILogger logger, Exception exception);
}
