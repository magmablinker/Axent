using Microsoft.CodeAnalysis;

namespace Axent.Generators;

internal static class TypeSymbolExtensions
{
    extension(INamedTypeSymbol symbol)
    {
        public bool IsRequestFamilyInterface()
            => symbol.IsAxentRequestInterface("IRequest`1")
               || symbol.IsAxentRequestInterface("ICommand`1")
               || symbol.IsAxentRequestInterface("IQuery`1")
               || symbol.IsAxentRequestInterface("ICacheableQuery`1");

        public bool IsCommandInterface()
            => symbol.IsAxentRequestInterface("ICommand`1");

        public bool IsCacheableQueryInterface()
            => symbol.IsAxentRequestInterface("ICacheableQuery`1");

        private bool IsAxentRequestInterface(string metadataName)
            => symbol is
            {
                MetadataName: var currentMetadataName,
                ContainingNamespace:
                {
                    Name: "Requests",
                    ContainingNamespace:
                    {
                        Name: "Abstractions",
                        ContainingNamespace:
                        {
                            Name: "Axent",
                            ContainingNamespace.IsGlobalNamespace: true
                        }
                    }
                }
            } && currentMetadataName == metadataName;
    }
}
