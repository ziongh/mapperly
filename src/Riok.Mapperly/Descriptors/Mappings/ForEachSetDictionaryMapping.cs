using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Descriptors.Enumerables;
using Riok.Mapperly.Descriptors.Enumerables.EnsureCapacity;
using Riok.Mapperly.Descriptors.Mappings.ExistingTarget;

namespace Riok.Mapperly.Descriptors.Mappings;

/// <summary>
/// Represents a foreach dictionary mapping which works by creating a new target instance,
/// looping through the source, mapping each element and setting it to the target collection.
/// </summary>
public class ForEachSetDictionaryMapping(
    CollectionInfos collectionInfos,
    INewInstanceMapping keyMapping,
    INewInstanceMapping valueMapping,
    INamedTypeSymbol? explicitCast,
    bool enableReferenceHandling
)
    : NewInstanceObjectMemberMethodMapping(collectionInfos.Source.Type, collectionInfos.Target.Type, enableReferenceHandling),
        INewInstanceEnumerableMapping
{
    private readonly ForEachSetDictionaryExistingTargetMapping _existingTargetMapping =
        new(collectionInfos, keyMapping, valueMapping, explicitCast);

    public CollectionInfos CollectionInfos => _existingTargetMapping.CollectionInfos;

    public void AddEnsureCapacity(EnsureCapacityInfo ensureCapacityInfo) => _existingTargetMapping.AddEnsureCapacity(ensureCapacityInfo);

    protected override IEnumerable<StatementSyntax> BuildBody(TypeMappingBuildContext ctx, ExpressionSyntax target)
    {
        return base.BuildBody(ctx, target).Concat(_existingTargetMapping.Build(ctx, target));
    }
}
