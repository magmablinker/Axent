namespace Axent.Abstractions.Caching;

/// <summary>
/// Ambient dimensions a cache entry is partitioned by.
/// </summary>
[Flags]
public enum CacheScope
{
    None = 0,

    /// <summary>
    /// One entry shared by every caller. Default.
    /// </summary>
    Global = 1 << 0,

    /// <summary>
    /// Partitioned by the current user identity.
    /// </summary>
    User = 1 << 1,

    /// <summary>
    /// Partitioned by the current tenant.
    /// </summary>
    Tenant = 1 << 2,

    /// <summary>
    /// Partitioned by the current UI culture.
    /// </summary>
    Culture = 1 << 3,
}
