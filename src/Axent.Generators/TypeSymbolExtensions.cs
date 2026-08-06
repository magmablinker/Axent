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

        public bool IsAuthorized()
        {
            var authorized = false;

            for (var current = symbol; current is not null; current = current.BaseType)
            {
                foreach (var attributeClass in current.GetAttributes().Select(attribute => attribute.AttributeClass).OfType<INamedTypeSymbol>())
                {
                    if (attributeClass.ImplementsAuthorizationMarker("IAllowAnonymous"))
                    {
                        return false;
                    }

                    if (attributeClass.ImplementsAuthorizationMarker("IAuthorizeData"))
                    {
                        authorized = true;
                    }
                }
            }

            return authorized;
        }

        public bool DeclaresCacheScope()
        {
            for (var current = symbol; current is not null; current = current.BaseType)
            {
                if (current.GetMembers().Any(member => StringComparer.Ordinal.Equals(member.Name, "CacheScope")
                                                       || member.Name.EndsWith(".CacheScope", StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ImplementsAuthorizationMarker(string markerName) =>
            symbol.IsAspNetCoreAuthorizationType(markerName) ||
            symbol.AllInterfaces.Any(candidate => candidate.IsAspNetCoreAuthorizationType(markerName));

        private bool IsAspNetCoreAuthorizationType(string name) =>
            StringComparer.Ordinal.Equals(symbol.Name, name)
            && symbol.ContainingNamespace
                .ToDisplayString()
                .StartsWith("Microsoft.AspNetCore.Authorization", StringComparison.Ordinal);

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
