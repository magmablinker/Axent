namespace Axent.Core.Pipes.Observability;

internal static class ActivityTags
{
    public const string ActivityId = "axent";
    public const string RequestType = $"{ActivityId}.request";
    public const string StackTrace = $"{ActivityId}.stacktrace";
    public const string ExceptionType = $"{ActivityId}.exception";
}
