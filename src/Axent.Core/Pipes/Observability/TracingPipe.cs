using System.Diagnostics;
using Axent.Abstractions;

namespace Axent.Core.Pipes.Observability;

internal sealed class TracingPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly IActivityFactory _activityFactory;

    public TracingPipe(IActivityFactory activityFactory)
    {
        _activityFactory = activityFactory;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        using var activity = _activityFactory.Create<TRequest>();
        if (activity is null)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        try
        {
            activity.SetTag(ActivityTags.RequestType, typeof(TRequest).Name);
            var result = await chain.NextAsync(context, cancellationToken);
            activity.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (OperationCanceledException)
        {
            activity.SetStatus(ActivityStatusCode.Unset);
            throw;
        }
        catch (Exception e)
        {
            activity.SetStatus(ActivityStatusCode.Error, e.Message);
            activity.SetTag(ActivityTags.ExceptionType, e.GetType().FullName);
            activity.SetTag(ActivityTags.StackTrace, e.StackTrace);
            throw;
        }
    }
}
