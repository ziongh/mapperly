using Microsoft.CodeAnalysis;

namespace Riok.Mapperly.Descriptors.Enumerables.EnsureCapacity;

/// <summary>
/// Generates an <see cref="EnsureCapacityInfo"/> of types <see cref="EnsureCapacityNonEnumerated"/> or <see cref="EnsureCapacityMember"/> depending on type information.
/// </summary>
public static class EnsureCapacityBuilder
{
    private const string EnsureCapacityName = "EnsureCapacity";
    private const string TryGetNonEnumeratedCountMethodName = "TryGetNonEnumeratedCount";

    public static EnsureCapacityInfo? TryBuildEnsureCapacity(MappingBuilderContext ctx) =>
        ctx.CollectionInfos == null ? null : TryBuildEnsureCapacity(ctx, ctx.CollectionInfos.Source, ctx.CollectionInfos.Target);

    public static EnsureCapacityInfo? TryBuildEnsureCapacity(MappingBuilderContext ctx, CollectionInfo source, CollectionInfo target)
    {
        var capacityMethod = ctx
            .SymbolAccessor.GetAllMethods(target.Type, EnsureCapacityName)
            .FirstOrDefault(x => x.Parameters.Length == 1 && x.Parameters[0].Type.SpecialType == SpecialType.System_Int32 && !x.IsStatic);

        // if EnsureCapacity is not available then return null
        if (capacityMethod == null)
            return null;

        // if target does not have a count then return null
        if (!target.CountIsKnown)
            return null;

        // if target and source count are known then create a simple EnsureCapacity statement
        if (source.CountIsKnown)
            return new EnsureCapacityMember(target.CountPropertyName, source.CountPropertyName);

        var nonEnumeratedCountMethod = ctx
            .Types.Get(typeof(Enumerable))
            .GetMembers(TryGetNonEnumeratedCountMethodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(x =>
                x.ReturnType.SpecialType == SpecialType.System_Boolean
                && x is { IsStatic: true, Parameters.Length: 2, IsGenericMethod: true }
            );

        // if non enumerated method doesnt exist then don't create EnsureCapacity
        if (nonEnumeratedCountMethod == null)
            return null;

        // if source does not have a count use GetNonEnumeratedCount, calling EnsureCapacity if count is available
        return new EnsureCapacityNonEnumerated(target.CountPropertyName, nonEnumeratedCountMethod);
    }
}
