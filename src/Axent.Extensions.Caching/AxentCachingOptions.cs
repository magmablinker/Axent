namespace Axent.Extensions.Caching;

public sealed class AxentCachingOptions
{
    /// <summary>
    /// What to do when a required scope discriminator cannot be resolved.
    /// </summary>
    public UnresolvedCacheScopeBehavior UnresolvedScopeBehavior { get; set; } =
        UnresolvedCacheScopeBehavior.Bypass;

    /// <summary>
    /// Emit an implicit per dimension tag on scoped entries so they can be evicted with
    /// <see cref="CacheScopeTags"/>. Off by default, because the in-memory provider keeps one
    /// expiration token per distinct tag until that tag is removed.
    /// </summary>
    public bool EmitScopeTags { get; set; }
}
