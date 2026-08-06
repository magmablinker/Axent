namespace Axent.Extensions.Caching;

/// <summary>
/// What to do when a cache scope discriminator cannot be resolved.
/// </summary>
/// <remarks>
/// Falling back to a global key is deliberately not offered. Sharing one entry across callers is
/// the exact defect that cache scopes exist to prevent, and the framework cannot tell a
/// legitimately anonymous caller apart from a misconfigured pipeline.
/// </remarks>
public enum UnresolvedCacheScopeBehavior
{
    /// <summary>
    /// Skip the cache and execute the handler. Default.
    /// </summary>
    Bypass = 0,

    /// <summary>
    /// Return a failure response.
    /// </summary>
    Fail = 1,
}
