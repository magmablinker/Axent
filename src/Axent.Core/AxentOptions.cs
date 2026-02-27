namespace Axent.Core;

public sealed class AxentOptions
{
    /// <summary>
    /// Determines whether to use the source-generated sender implementation.
    /// Defaults to true.
    /// </summary>
    public bool UseSourceGeneratedSender { get; set; } = true;
}
