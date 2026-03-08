namespace Axent.Core.DependencyInjection;

/// <summary>
/// Configures logging behavior within the Axent pipeline.
/// </summary>
public sealed class AxentLoggingOptions
{
    /// <summary>
    /// When <see langword="true"/>, the full request object is logged via <see cref="Microsoft.Extensions.Logging.ILogger"/> before processing.
    /// Avoid enabling in production if requests may contain sensitive data.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableRequestLogging { get; set; }
}
