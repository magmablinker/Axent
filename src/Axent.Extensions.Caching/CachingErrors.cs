using System.Net;
using Axent.Abstractions.Caching;
using Axent.Abstractions.Models;

namespace Axent.Extensions.Caching;

public static class CachingErrors
{
    public static Error UnresolvedCacheScope(CacheScope scope) =>
        new(
            $"{nameof(Caching)}.{nameof(UnresolvedCacheScope)}",
            HttpStatusCode.InternalServerError,
            $"Could not resolve the cache scope discriminator for '{scope}'.");
}
