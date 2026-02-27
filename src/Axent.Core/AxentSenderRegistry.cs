using Axent.Abstractions;

namespace Axent.Core;

/// <summary>
/// A static registry that the source-generated module initializer writes into
/// before any user code runs. This allows <see cref="AxentServiceRegistration"/>
/// to register the generated <c>ISender</c> without scanning assemblies or
/// relying on naming conventions.
/// </summary>
public static class AxentSenderRegistry
{
    private static Func<IServiceProvider, ISender>? _factory;

    /// <summary>
    /// Called exclusively by the source-generated module initializer.
    /// </summary>
    public static void Register(Func<IServiceProvider, ISender> factory)
    {
        if (_factory is not null)
        {
            throw new AxentConfigurationException(
                "An ISender factory has already been registered. " +
                "Ensure only one assembly references Axent.SourceGenerator.");
        }

        _factory = factory;
    }

    internal static Func<IServiceProvider, ISender> GetFactory() =>
        _factory ?? throw new AxentConfigurationException(
            "No generated ISender was found. " +
            "Ensure the Axent.SourceGenerator package is referenced and the project has been built.");
}
