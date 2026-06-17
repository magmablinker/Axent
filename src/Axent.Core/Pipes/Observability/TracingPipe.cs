using System.Diagnostics;
using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;

namespace Axent.Core.Pipes.Observability;

internal sealed class TracingPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly IActivityFactory _activityFactory;

    public TracingPipe(IActivityFactory activityFactory)
    {
        _activityFactory = activityFactory;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activityFactory.Create<TRequest>();
        if (activity is null)
        {
            return await next(request, cancellationToken);
        }

        try
        {
            activity.SetTag(ActivityTags.RequestType, typeof(TRequest).Name);
            var result = await next(request, cancellationToken);
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
