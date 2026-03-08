using System.Diagnostics;
using Axent.Abstractions;

namespace Axent.Core.Pipes.Observability;

internal sealed class TracingPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private static readonly Type _requestType = typeof(TRequest);

    public async ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.ActivitySource.StartActivity(_requestType.FullName ?? _requestType.Name);

        try
        {
            activity?.SetTag(ActivityTags.RequestType, typeof(TRequest).Name);
            var result = await chain.NextAsync(context, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (OperationCanceledException o)
        {
            activity?.SetStatus(ActivityStatusCode.Unset);
            activity?.SetTag(ActivityTags.ExceptionType, o.GetType().FullName);
            throw;
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.SetTag(ActivityTags.ExceptionType, e.GetType().FullName);
            activity?.SetTag(ActivityTags.StackTrace, e.StackTrace);
            throw;
        }
    }
}

internal static class Tracing
{
    public static readonly ActivitySource ActivitySource = new(ActivityTags.ActivityId);
}
