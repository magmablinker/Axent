namespace Axent.Core.Observability;

internal static class ActivityTags
{
    public const string ActivityId = "axent";
    public const string RequestType = $"{ActivityId}.request";
    public const string StackTrace = $"{ActivityId}.stacktrace";
}
