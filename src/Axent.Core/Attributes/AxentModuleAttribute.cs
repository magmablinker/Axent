using Microsoft.Extensions.DependencyInjection;

namespace Axent.Core.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AxentModuleAttribute : Attribute
{
    public AxentModuleAttribute(Type registrarType)
    {
        RegistrarType = registrarType;
    }

    public Type RegistrarType { get; }
}

public interface IAxentModuleRegistrar
{
    void Register(IServiceCollection services);
}
