using Microsoft.CodeAnalysis;

namespace Riok.Mapperly.Helpers;

/// <summary>
/// The fork's scalar-member policy. Deliberately independent of the mapping builders:
/// a namespace named System does not make arbitrary objects safe scalar values.
/// </summary>
internal static class PrimitiveMemberType
{
    internal static bool IsSupported(ITypeSymbol type)
    {
        if (IsScalar(type))
            return true;

        if (type is IArrayTypeSymbol array)
            return array.Rank == 1 && IsScalar(array.ElementType);

        if (type is not INamedTypeSymbol named)
            return false;

        foreach (var candidate in named.AllInterfaces.Concat([named]))
        {
            if (
                string.Equals(candidate.ContainingNamespace.ToDisplayString(), "System.Collections.Generic", StringComparison.Ordinal)
                && candidate.MetadataName is "IDictionary`2" or "IReadOnlyDictionary`2"
            )
            {
                return candidate.TypeArguments.All(IsScalar);
            }
        }

        return named
            .AllInterfaces.Concat([named])
            .Any(candidate =>
                candidate.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                && IsScalar(candidate.TypeArguments[0])
            );
    }

    private static bool IsScalar(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return IsScalar(named.TypeArguments[0]);

        // Includes strongly typed numeric/Guid keys without depending on an application-specific interface.
        // Unlike arbitrary structs, unmanaged structs cannot hide lists or entity references in private fields.
        if (type.TypeKind == TypeKind.Enum || type is INamedTypeSymbol { TypeKind: TypeKind.Struct, IsUnmanagedType: true })
            return true;

        if (
            type.SpecialType
            is SpecialType.System_Boolean
                or SpecialType.System_Char
                or SpecialType.System_SByte
                or SpecialType.System_Byte
                or SpecialType.System_Int16
                or SpecialType.System_UInt16
                or SpecialType.System_Int32
                or SpecialType.System_UInt32
                or SpecialType.System_Int64
                or SpecialType.System_UInt64
                or SpecialType.System_IntPtr
                or SpecialType.System_UIntPtr
                or SpecialType.System_Single
                or SpecialType.System_Double
                or SpecialType.System_Decimal
                or SpecialType.System_String
        )
        {
            return true;
        }

        return string.Equals(type.ContainingNamespace?.ToDisplayString(), "System", StringComparison.Ordinal)
            && type.MetadataName
                is "Guid"
                    or "DateTime"
                    or "DateTimeOffset"
                    or "DateOnly"
                    or "TimeOnly"
                    or "TimeSpan"
                    or "Half"
                    or "Int128"
                    or "UInt128";
    }
}
