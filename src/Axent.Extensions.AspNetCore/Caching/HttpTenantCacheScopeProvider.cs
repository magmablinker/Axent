using Axent.Abstractions.Caching;
using Microsoft.AspNetCore.Http;

namespace Axent.Extensions.AspNetCore.Caching;

/// <summary>
/// Resolves <see cref="CacheScope.Tenant"/> from a claim on the current <see cref="HttpContext"/>.
/// </summary>
/// <remarks>
/// Unlike the user provider this does not require an authenticated principal, because
/// unauthenticated multi-tenant endpoints are legitimate.
/// </remarks>
internal sealed class HttpTenantCacheScopeProvider(
    IHttpContextAccessor httpContextAccessor,
    HttpCacheScopeOptions options) : ICacheScopeProvider
{
    public CacheScope Scope => CacheScope.Tenant;

    public ValueTask<string?> GetDiscriminatorAsync(CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user is null)
        {
            return ValueTask.FromResult<string?>(null);
        }

        foreach (var claimType in options.TenantClaimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return ValueTask.FromResult<string?>(value);
            }
        }

        return ValueTask.FromResult<string?>(null);
    }
}
