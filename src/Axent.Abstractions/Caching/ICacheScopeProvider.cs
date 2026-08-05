namespace Axent.Abstractions.Caching;

/// <summary>
/// Resolves the ambient discriminator for a single <see cref="CacheScope"/> dimension.
/// </summary>
/// <remarks>
/// Implementations are resolved per dependency injection scope and must read ambient state
/// inside <see cref="GetDiscriminatorAsync"/>, never in their constructor.
/// </remarks>
public interface ICacheScopeProvider
{
    /// <summary>
    /// The single dimension this provider resolves.
    /// </summary>
    CacheScope Scope { get; }

    /// <summary>
    /// Returns the discriminator for the current ambient state,
    /// or <c>null</c> when no value is available.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation</param>
    ValueTask<string?> GetDiscriminatorAsync(CancellationToken cancellationToken = default);
}
