using System.Reflection;

namespace Axent.Templates.MinimalApi;

internal static class AssemblyProvider
{
    public static readonly Assembly Current = typeof(AssemblyProvider).Assembly;
}
