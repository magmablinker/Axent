using Microsoft.CodeAnalysis;

namespace Axent.Generators;

internal static class TypeSymbolExtensions
{
    extension(INamedTypeSymbol symbol)
    {
        public bool IsRequestFamilyInterface()
        {
            if (symbol.TypeArguments.Length != 1)
            {
                return false;
            }

            var originalDefinition = symbol.OriginalDefinition;

            return originalDefinition.IsAxentAbstractionsInterface("IRequest`1")
                   || originalDefinition.IsAxentAbstractionsInterface("ICommand`1")
                   || originalDefinition.IsAxentAbstractionsInterface("IQuery`1")
                   || originalDefinition.IsAxentAbstractionsInterface("ICacheableQuery`1");
        }

        public bool IsCommandInterface()
        {
            return symbol.OriginalDefinition.IsAxentAbstractionsInterface("ICommand`1");
        }

        public bool IsCacheableQueryInterface()
        {
            return symbol.OriginalDefinition.IsAxentAbstractionsInterface("ICacheableQuery`1");
        }

        private bool IsAxentAbstractionsInterface(string metadataName)
        {
            if (!StringComparer.Ordinal.Equals(symbol.MetadataName, metadataName))
            {
                return false;
            }

            var containingNamespace = symbol.ContainingNamespace.ToDisplayString();

            return containingNamespace.StartsWith(
                "Axent.Abstractions",
                StringComparison.Ordinal);
        }
    }
}
