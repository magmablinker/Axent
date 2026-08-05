using Axent.Abstractions.Caching;

namespace Axent.Extensions.Caching;

/// <summary>
/// The known <see cref="CacheScope"/> dimensions and their key abbreviations.
/// </summary>
internal static class CacheScopeDimensions
{
    /// <summary>
    /// Every known dimension, in ascending flag order. Composition walks this array so that a key
    /// is stable regardless of the order scope providers happen to be registered in.
    /// </summary>
    public static readonly CacheScope[] Ordered =
    [
        CacheScope.User,
        CacheScope.Tenant,
        CacheScope.Culture,
    ];

    public const CacheScope Known = CacheScope.User | CacheScope.Tenant | CacheScope.Culture;

    public static string Abbreviate(CacheScope scope) => scope switch
    {
        CacheScope.User => "u",
        CacheScope.Tenant => "t",
        CacheScope.Culture => "c",
        _ => scope.ToString().ToLowerInvariant(),
    };
}
