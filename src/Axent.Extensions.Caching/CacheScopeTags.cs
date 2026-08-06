using Axent.Abstractions.Caching;

namespace Axent.Extensions.Caching;

/// <summary>
/// Builds the implicit per dimension tags emitted on scoped cache entries when
/// <see cref="AxentCachingOptions.EmitScopeTags"/> is enabled.
/// </summary>
/// <remarks>
/// Use these to evict everything cached for one user or tenant, for example on logout or
/// offboarding, via <see cref="ICache.RemoveByTagsAsync"/>.
/// </remarks>
public static class CacheScopeTags
{
    internal const string Prefix = "axent:scope:";

    /// <summary>
    /// Builds the tag for a single dimension and discriminator.
    /// </summary>
    /// <param name="scope">A single <see cref="CacheScope"/> dimension</param>
    /// <param name="discriminator">The resolved discriminator</param>
    public static string For(CacheScope scope, string discriminator) =>
        $"{Prefix}{CacheScopeDimensions.Abbreviate(scope)}={CacheKeyEscaper.Escape(discriminator)}";

    /// <summary>
    /// Builds the tag covering entries with <paramref name="tag"/> for one scope discriminator.
    /// </summary>
    /// <param name="scope">A single <see cref="CacheScope"/> dimension</param>
    /// <param name="discriminator">The resolved discriminator</param>
    /// <param name="tag">The request-level cache tag</param>
    public static string ForTag(CacheScope scope, string discriminator, string tag)
    {
        ArgumentNullException.ThrowIfNull(discriminator);
        ArgumentNullException.ThrowIfNull(tag);

        if (!CacheScopeDimensions.IsSingleDimension(scope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scope),
                scope,
                "A single user, tenant, or culture cache scope is required.");
        }

        return Combine(For(scope, discriminator), tag);
    }

    /// <summary>
    /// Builds the tag covering every entry cached for one user.
    /// </summary>
    /// <param name="userId">The user discriminator</param>
    public static string User(string userId) => For(CacheScope.User, userId);

    /// <summary>
    /// Builds the tag covering entries with <paramref name="tag"/> for one user.
    /// </summary>
    /// <param name="userId">The user discriminator</param>
    /// <param name="tag">The request-level cache tag</param>
    public static string UserTag(string userId, string tag) =>
        ForTag(CacheScope.User, userId, tag);

    /// <summary>
    /// Builds the tag covering every entry cached for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant discriminator</param>
    public static string Tenant(string tenantId) => For(CacheScope.Tenant, tenantId);

    /// <summary>
    /// Builds the tag covering entries with <paramref name="tag"/> for one tenant.
    /// </summary>
    /// <param name="tenantId">The tenant discriminator</param>
    /// <param name="tag">The request-level cache tag</param>
    public static string TenantTag(string tenantId, string tag) =>
        ForTag(CacheScope.Tenant, tenantId, tag);

    internal static string Combine(string scopeTag, string tag)
    {
        ArgumentNullException.ThrowIfNull(scopeTag);
        ArgumentNullException.ThrowIfNull(tag);

        return $"{scopeTag}|tag={CacheKeyEscaper.Escape(tag)}";
    }
}
