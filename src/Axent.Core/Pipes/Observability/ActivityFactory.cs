using System.Diagnostics;

namespace Axent.Core.Pipes.Observability;

internal sealed class ActivityFactory : IActivityFactory
{
    private static readonly ActivitySource _activitySource = new(ActivityTags.ActivityId);

    public Activity? Create<TRequest>()
    {
        return _activitySource.StartActivity(typeof(TRequest).Name);
    }
}
