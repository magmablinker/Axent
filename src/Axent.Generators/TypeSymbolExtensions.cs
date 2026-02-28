using Microsoft.CodeAnalysis;

namespace Axent.Generators;

internal static class TypeSymbolExtensions
{
    public static bool IsRequestInterface(this INamedTypeSymbol symbol)
        => symbol is
        {
            MetadataName: "IRequest`1",
            ContainingNamespace:
            {
                Name: "Abstractions",
                ContainingNamespace:
                {
                    Name: "Axent",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };
}
