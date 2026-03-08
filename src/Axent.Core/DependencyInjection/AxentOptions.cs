namespace Axent.Core.DependencyInjection;

/// <summary>
/// Root configuration options for the Axent pipeline.
/// </summary>
public sealed class AxentOptions
{
    /// <summary>
    /// Configures error handling behavior for the pipeline.
    /// When <see langword="null"/>, exceptions are not caught and propagate directly to the caller.
    /// </summary>
    public AxentErrorHandlingOptions? ErrorHandling { get; set; }

    /// <summary>
    /// Configures logging behavior for the pipeline.
    /// </summary>
    public AxentLoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Configures transaction behavior for command handlers.
    /// </summary>
    public AxentTransactionOptions Transactions { get; set; } = new();
}
