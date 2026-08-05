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

        /// <summary>
        /// Whether the type, or a base type, carries an attribute that behaves like
        /// <c>[Authorize]</c>, and is not suppressed by <c>[AllowAnonymous]</c>.
        /// </summary>
        /// <remarks>
        /// Detected structurally, by looking for the ASP.NET Core marker interfaces on the
        /// attribute class, so the generator never needs to reference ASP.NET Core.
        /// </remarks>
        public bool IsAuthorized()
        {
            var authorized = false;

            for (var current = symbol; current is not null; current = current.BaseType)
            {
                foreach (var attribute in current.GetAttributes())
                {
                    var attributeClass = attribute.AttributeClass;

                    if (attributeClass is null)
                    {
                        continue;
                    }

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

        /// <summary>
        /// Whether the type, or a base type, declares its own <c>CacheScope</c> member. When it
        /// does not, the default interface member is in effect and the entry is global.
        /// </summary>
        public bool DeclaresCacheScope()
        {
            for (var current = symbol; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    // Explicit interface implementations are named
                    // 'Axent.Abstractions.Requests.ICacheableQuery<T>.CacheScope'.
                    if (StringComparer.Ordinal.Equals(member.Name, "CacheScope")
                        || member.Name.EndsWith(".CacheScope", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ImplementsAuthorizationMarker(string markerName)
        {
            if (symbol.IsAspNetCoreAuthorizationType(markerName))
            {
                return true;
            }

            foreach (var candidate in symbol.AllInterfaces)
            {
                if (candidate.IsAspNetCoreAuthorizationType(markerName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAspNetCoreAuthorizationType(string name)
        {
            return StringComparer.Ordinal.Equals(symbol.Name, name)
                   && symbol.ContainingNamespace
                       .ToDisplayString()
                       .StartsWith("Microsoft.AspNetCore.Authorization", StringComparison.Ordinal);
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
