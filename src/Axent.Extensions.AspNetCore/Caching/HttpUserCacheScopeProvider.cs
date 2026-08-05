using System.Security.Claims;
using Axent.Abstractions.Caching;
using Microsoft.AspNetCore.Http;

namespace Axent.Extensions.AspNetCore.Caching;

/// <summary>
/// Resolves <see cref="CacheScope.User"/> from the authenticated principal on the current
/// <see cref="HttpContext"/>.
/// </summary>
/// <remarks>
/// Returns <c>null</c> for anonymous callers rather than a shared sentinel. Anonymous callers are
/// mutually indistinguishable, but they are also indistinguishable from a pipeline where
/// authentication has not run, so the safe outcome is to leave the scope unresolved. Register a
/// replacement provider to deliberately cache anonymous traffic.
/// </remarks>
internal sealed class HttpUserCacheScopeProvider(
    IHttpContextAccessor httpContextAccessor,
    HttpCacheScopeOptions options) : ICacheScopeProvider
{
    public CacheScope Scope => CacheScope.User;

    public ValueTask<string?> GetDiscriminatorAsync(CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated is not true)
        {
            return ValueTask.FromResult<string?>(null);
        }

        return ValueTask.FromResult(FindClaimValue(user, options.UserClaimTypes));
    }

    private static string? FindClaimValue(ClaimsPrincipal principal, IReadOnlyList<string> claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
