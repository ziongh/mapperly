using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Riok.Mapperly.Helpers;

internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat _fullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

    private static readonly ImmutableHashSet<string> _wellKnownImmutableTypes = ImmutableHashSet.Create(
        typeof(Uri).FullName!,
        typeof(Version).FullName!
    );

    internal static Location? GetSyntaxLocation(this ISymbol symbol) =>
        symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();

    internal static string FullyQualifiedIdentifierName(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(_fullyQualifiedNullableFormat);

    internal static bool IsImmutable(this ISymbol symbol)
    {
        if (symbol is not INamedTypeSymbol namedSymbol)
            return false;

        return namedSymbol.IsUnmanagedType
            || namedSymbol.SpecialType == SpecialType.System_String
            || IsDelegate(symbol)
            || _wellKnownImmutableTypes.Contains(namedSymbol.ToDisplayString());
    }

    internal static bool IsDelegate(this ISymbol symbol)
    {
        if (symbol is not INamedTypeSymbol namedSymbol)
            return false;

        return namedSymbol.DelegateInvokeMethod != null;
    }

    internal static int GetInheritanceLevel(this ITypeSymbol symbol)
    {
        var level = 0;
        while (symbol.BaseType != null)
        {
            symbol = symbol.BaseType;
            level++;
        }

        return level;
    }

    internal static bool IsArrayType(this ITypeSymbol symbol) => symbol is IArrayTypeSymbol;

    internal static bool IsArrayType(this ITypeSymbol symbol, [NotNullWhen(true)] out IArrayTypeSymbol? arrayTypeSymbol)
    {
        arrayTypeSymbol = symbol as IArrayTypeSymbol;
        return arrayTypeSymbol != null;
    }

    internal static bool IsEnum(this ITypeSymbol t) => TryGetEnumUnderlyingType(t, out _);

    internal static bool TryGetEnumUnderlyingType(this ITypeSymbol t, [NotNullWhen(true)] out INamedTypeSymbol? enumType)
    {
        enumType = (t.NonNullable() as INamedTypeSymbol)?.EnumUnderlyingType;
        return enumType != null;
    }

    internal static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol symbol) => symbol.GetMembers().OfType<IFieldSymbol>();

    internal static IMethodSymbol? GetStaticGenericMethod(this INamedTypeSymbol namedType, string methodName)
    {
        return namedType.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault(m => m.IsStatic && m.IsGenericMethod);
    }

    internal static bool Implements(this ITypeSymbol t, INamedTypeSymbol interfaceSymbol)
    {
        return SymbolEqualityComparer.Default.Equals(t, interfaceSymbol)
            || t.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, interfaceSymbol));
    }

    internal static bool ExtendsOrImplementsGeneric(
        this ITypeSymbol t,
        INamedTypeSymbol genericSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? typedGenericSymbol
    )
    {
        return genericSymbol.TypeKind == TypeKind.Interface
            ? t.ImplementsGeneric(genericSymbol, out typedGenericSymbol)
            : t.ExtendsGeneric(genericSymbol, out typedGenericSymbol);
    }

    internal static bool ExtendsGeneric(
        this ITypeSymbol t,
        INamedTypeSymbol genericSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? typedGenericSymbol
    )
    {
        Debug.Assert(genericSymbol.IsGenericType);

        if (SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, genericSymbol))
        {
            typedGenericSymbol = (INamedTypeSymbol)t;
            return true;
        }

        for (var baseType = t.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            if (!SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, genericSymbol))
                continue;

            typedGenericSymbol = baseType;
            return true;
        }

        typedGenericSymbol = null;
        return false;
    }

    internal static bool ImplementsGeneric(
        this ITypeSymbol t,
        INamedTypeSymbol genericInterfaceSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? typedInterface
    )
    {
        Debug.Assert(genericInterfaceSymbol.IsGenericType);
        Debug.Assert(genericInterfaceSymbol.TypeKind == TypeKind.Interface);

        if (SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, genericInterfaceSymbol))
        {
            typedInterface = (INamedTypeSymbol)t;
            return true;
        }

        foreach (var typeSymbol in t.AllInterfaces)
        {
            if (typeSymbol.IsGenericType && SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, genericInterfaceSymbol))
            {
                typedInterface = typeSymbol;
                return true;
            }
        }

        typedInterface = null;
        return false;
    }

    internal static bool ImplementsGeneric(
        this ITypeSymbol t,
        INamedTypeSymbol genericInterfaceSymbol,
        string symbolName,
        [NotNullWhen(true)] out INamedTypeSymbol? typedInterface,
        out bool isExplicit
    )
    {
        Debug.Assert(genericInterfaceSymbol.IsGenericType);
        Debug.Assert(genericInterfaceSymbol.TypeKind == TypeKind.Interface);

        if (SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, genericInterfaceSymbol))
        {
            typedInterface = (INamedTypeSymbol)t;
            isExplicit = false;
            return true;
        }

        typedInterface = t.AllInterfaces.FirstOrDefault(x =>
            x.IsGenericType && SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, genericInterfaceSymbol)
        );

        if (typedInterface == null)
        {
            isExplicit = false;
            return false;
        }

        if (t.IsAbstract)
        {
            isExplicit = false;
            return true;
        }

        var interfaceSymbol = typedInterface.GetMembers(symbolName).First();
        var symbolImplementation = t.FindImplementationForInterfaceMember(interfaceSymbol);

        // if null then the method is unimplemented
        // symbol implements genericInterface but has not implemented the corresponding methods
        // this is for example the case for arrays (arrays implement several interfaces at runtime)
        // and unit tests for which not the full interface is implemented
        if (symbolImplementation == null)
        {
            isExplicit = false;
            return false;
        }

        // check if symbol is explicit
        isExplicit = symbolImplementation switch
        {
            IMethodSymbol methodSymbol => methodSymbol.ExplicitInterfaceImplementations.Any(),
            IPropertySymbol propertySymbol => propertySymbol.ExplicitInterfaceImplementations.Any(),
            _ => throw new NotSupportedException(symbolImplementation.GetType().Name + " is not supported"),
        };

        return true;
    }

    internal static bool HasImplicitGenericImplementation(this ITypeSymbol symbol, INamedTypeSymbol inter, string methodName) =>
        symbol.ImplementsGeneric(inter, methodName, out _, out var isExplicit) && !isExplicit;
}
