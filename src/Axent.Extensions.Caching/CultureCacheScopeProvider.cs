using System.Globalization;
using Axent.Abstractions.Caching;

namespace Axent.Extensions.Caching;

/// <summary>
/// Resolves <see cref="CacheScope.Culture"/> from <see cref="CultureInfo.CurrentUICulture"/>.
/// </summary>
internal sealed class CultureCacheScopeProvider : ICacheScopeProvider
{
    private const string Invariant = "invariant";

    public CacheScope Scope => CacheScope.Culture;

    public ValueTask<string?> GetDiscriminatorAsync(CancellationToken cancellationToken = default)
    {
        var name = CultureInfo.CurrentUICulture.Name;

        return ValueTask.FromResult<string?>(
            string.IsNullOrEmpty(name) ? Invariant : name);
    }
}
