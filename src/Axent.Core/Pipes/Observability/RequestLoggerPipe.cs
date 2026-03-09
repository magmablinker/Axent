using System.Diagnostics;
using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.Logging;

namespace Axent.Core.Pipes.Observability;

internal sealed class RequestLoggerPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private static readonly Type _requestType = typeof(TRequest);
    private static readonly Type _responseType = typeof(TResponse);

    private readonly ILogger<RequestLoggerPipe<TRequest, TResponse>> _logger;

    public RequestLoggerPipe(ILogger<RequestLoggerPipe<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.RequestStarted(_requestType.Name);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await chain.NextAsync(context, cancellationToken);
            _logger.RequestFinished(_requestType.Name, sw.ElapsedMilliseconds, _responseType.Name);
            return response;
        }
        finally
        {
            sw.Stop();
        }
    }
}

internal static partial class RequestLoggerPipeLoggerExtensions
{
    [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Debug,
            Message = "Starting processing of request with type '{requestType}'"),
    ]
    public static partial void RequestStarted(
        this ILogger logger, string requestType);

    [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "Request '{requestType}' has finished processing ({duration}ms). Returning response with type '{responseType}'."),
    ]
    public static partial void RequestFinished(
        this ILogger logger, string requestType, long duration, string responseType);
}
