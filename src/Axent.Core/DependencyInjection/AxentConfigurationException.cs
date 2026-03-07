using Axent.Abstractions;

namespace Axent.Core.DependencyInjection;

public sealed class AxentConfigurationException(string message) : AxentException(message);
