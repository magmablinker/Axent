using System.Security.Claims;

namespace Axent.Extensions.AspNetCore.Caching;

/// <summary>
/// Configures which claims the built-in HTTP cache scope providers read.
/// </summary>
public sealed class HttpCacheScopeOptions
{
    /// <summary>
    /// Claim types checked in order to resolve the user discriminator.
    /// </summary>
    public IReadOnlyList<string> UserClaimTypes { get; set; } = [ClaimTypes.NameIdentifier, "sub"];

    /// <summary>
    /// Claim types checked in order to resolve the tenant discriminator.
    /// </summary>
    public IReadOnlyList<string> TenantClaimTypes { get; set; } = ["tenant_id", "tid"];
}
