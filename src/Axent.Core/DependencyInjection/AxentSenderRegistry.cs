using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.DependencyInjection;

/// <summary>
/// A static registry that the source-generated module initializer writes into
/// before any user code runs. Accepts a registration action rather than a factory
/// so that all service registrations (ISender + all pipeline types) are applied
/// to the IServiceCollection at AddAxent() time, before the container is built.
/// </summary>
public static class AxentSenderRegistry
{
    private static Action<IServiceCollection>? _registration;

    /// <summary>
    /// Called exclusively by the source-generated module initializer.
    /// </summary>
    public static void Register(Action<IServiceCollection> registration)
    {
        if (_registration is not null)
        {
            throw new AxentConfigurationException("A sender registration has already been registered. Ensure only one assembly references Axent.SourceGenerator.");
        }

        _registration = registration;
    }

    internal static void Apply(IServiceCollection services) =>
            (_registration ?? throw new AxentConfigurationException("No generated sender registration was found. Ensure the Axent.SourceGenerator package is referenced and the project has been built."))
        .Invoke(services);
}
