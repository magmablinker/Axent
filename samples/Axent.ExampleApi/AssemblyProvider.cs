using System.Reflection;

namespace Axent.ExampleApi;

internal static class AssemblyProvider
{
    public static readonly Assembly Current = typeof(AssemblyProvider).Assembly;
}
