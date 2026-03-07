namespace Axent.Core.DependencyInjection;

public sealed class AxentOptions
{
    /// <summary>
    /// Determines whether error handling is enabled or exceptions should be "forwarded" to the consumer
    /// of the library.
    /// No errors will be caught if the options are not set.
    /// </summary>
    public AxentErrorHandlingOptions? ErrorHandling { get; set; }

    /// <summary>
    /// Provide logging related configurations
    /// </summary>
    public AxentLoggingOptions Logging { get; set; } = new ();
}

public sealed class AxentErrorHandlingOptions
{
    /// <summary>
    /// Determines whether the full exception gets returned or not, when the `ErrorHandlingPipe` is registered.
    /// Defaults to false.
    /// </summary>
    public bool EnableDetailedExceptionResponse { get; set; }
}

public sealed class AxentLoggingOptions
{
    /// <summary>
    /// Determines whether the full request gets logged via ILogger.
    /// Defaults to false.
    /// </summary>
    public bool EnableRequestLogging { get; set; }
}
