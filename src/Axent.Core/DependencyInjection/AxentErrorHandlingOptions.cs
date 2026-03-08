namespace Axent.Core.DependencyInjection;

/// <summary>
/// Configures error handling behavior within the Axent pipeline.
/// Requires the <c>ErrorHandlingPipe</c> to be registered.
/// </summary>
public sealed class AxentErrorHandlingOptions
{
    /// <summary>
    /// When <see langword="true"/>, the full exception details are included in the error response.
    /// When <see langword="false"/>, only a generic error is returned, which is recommended for production environments.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableDetailedExceptionResponse { get; set; }
}
