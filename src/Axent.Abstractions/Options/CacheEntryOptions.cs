namespace Axent.Abstractions.Options;

public sealed record CacheEntryOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}
