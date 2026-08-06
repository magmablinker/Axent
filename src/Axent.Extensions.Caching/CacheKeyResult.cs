using System.Diagnostics.CodeAnalysis;
using Axent.Abstractions.Caching;

namespace Axent.Extensions.Caching;

/// <summary>
/// Outcome of composing a cache key from a request key and its <see cref="CacheScope"/>.
/// </summary>
public readonly struct CacheKeyResult
{
    /// <summary>
    /// The composed cache key, or <c>null</c> when a required discriminator could not be resolved.
    /// </summary>
    /// <remarks>
    /// The format is an implementation detail. Do not parse it or depend on it across versions.
    /// </remarks>
    public string? Key { get; init; }

    /// <summary>
    /// Implicit per dimension tags to add to the entry, empty unless
    /// <see cref="AxentCachingOptions.EmitScopeTags"/> is enabled.
    /// </summary>
    public IReadOnlyList<string> ScopeTags { get; init; }

    /// <summary>
    /// The dimensions that could not be resolved, <see cref="CacheScope.None"/> when resolved.
    /// </summary>
    public CacheScope UnresolvedScope { get; init; }

    /// <summary>
    /// Whether a key was composed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Key))]
    public bool IsResolved => Key is not null;

    /// <summary>
    /// Creates a resolved result.
    /// </summary>
    /// <param name="key">The composed cache key</param>
    /// <param name="scopeTags">Implicit per dimension tags to add to the entry</param>
    public static CacheKeyResult Resolved(string key, IReadOnlyList<string> scopeTags) =>
        new() { Key = key, ScopeTags = scopeTags, UnresolvedScope = CacheScope.None };

    /// <summary>
    /// Creates an unresolved result.
    /// </summary>
    /// <param name="scope">The dimensions that could not be resolved</param>
    public static CacheKeyResult Unresolved(CacheScope scope) =>
        new() { Key = null, ScopeTags = [], UnresolvedScope = scope };
}
