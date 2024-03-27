using Microsoft.CodeAnalysis;
using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Descriptors.Enumerables;
using Riok.Mapperly.Descriptors.Enumerables.EnsureCapacity;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.ExistingTarget;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Helpers;

namespace Riok.Mapperly.Descriptors.MappingBuilders;

public static class DictionaryMappingBuilder
{
    private const string SetterIndexerPropertyName = "set_Item";

    private const string ToImmutableDictionaryMethodName = "global::System.Collections.Immutable.ImmutableDictionary.ToImmutableDictionary";
    private const string ToImmutableSortedDictionaryMethodName =
        "global::System.Collections.Immutable.ImmutableSortedDictionary.ToImmutableSortedDictionary";

    public static INewInstanceMapping? TryBuildMapping(MappingBuilderContext ctx)
    {
        if (!ctx.IsConversionEnabled(MappingConversionType.Dictionary))
            return null;

        if (ctx.DictionaryInfos == null)
            return null;

        if (BuildKeyValueMapping(ctx) is not var (keyMapping, valueMapping))
            return null;

        // target is of type IDictionary<,>, IReadOnlyDictionary<,> or Dictionary<,>.
        // The constructed type should be Dictionary<,>
        if (
            ctx.CollectionInfos?.Target.CollectionType
            is CollectionType.Dictionary
                or CollectionType.IDictionary
                or CollectionType.IReadOnlyDictionary
        )
        {
            return BuildDictionaryMapping(ctx, keyMapping, valueMapping);
        }

        // if target is an immutable dictionary then use LinqDictionaryMapper
        var immutableLinqMapping = BuildImmutableMapping(ctx, keyMapping, valueMapping);
        if (immutableLinqMapping != null)
            return immutableLinqMapping;

        return BuildCustomTypeMapping(ctx, keyMapping, valueMapping);
    }

    private static INewInstanceMapping? BuildCustomTypeMapping(
        MappingBuilderContext ctx,
        INewInstanceMapping keyMapping,
        INewInstanceMapping valueMapping
    )
    {
        // the target is not a well known dictionary type
        // it should have a an object factory or a parameterless public ctor
        var hasObjectFactory = ctx.ObjectFactories.TryFindObjectFactory(ctx.Source, ctx.Target, out var objectFactory);
        if (!hasObjectFactory && !ctx.SymbolAccessor.HasDirectlyAccessibleParameterlessConstructor(ctx.Target))
        {
            ctx.ReportDiagnostic(DiagnosticDescriptors.NoParameterlessConstructorFound, ctx.Target);
            return null;
        }

        if (!ctx.CollectionInfos!.Target.ImplementedTypes.HasFlag(CollectionType.IDictionary))
            return null;

        var sourceCollectionInfo = ctx.CollectionInfos.Source;
        if (!hasObjectFactory)
        {
            sourceCollectionInfo = BuildCollectionTypeForSourceIDictionary(ctx);
            ctx.ObjectFactories.TryFindObjectFactory(ctx.Source, ctx.Target, out objectFactory);

            var existingMapping = ctx.BuildDelegatedMapping(sourceCollectionInfo.Type, ctx.Target);
            if (existingMapping != null)
                return existingMapping;
        }

        var ensureCapacityStatement = EnsureCapacityBuilder.TryBuildEnsureCapacity(ctx, sourceCollectionInfo, ctx.CollectionInfos.Target);
        return new ForEachSetDictionaryMapping(
            sourceCollectionInfo.Type,
            ctx.Target,
            keyMapping,
            valueMapping,
            false,
            objectFactory: objectFactory,
            explicitCast: GetExplicitIndexer(ctx),
            ensureCapacity: ensureCapacityStatement
        );
    }

    /// <summary>
    /// Builds a for each set mapping for a dictionary.
    /// Target type needs to be assignable from <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    private static INewInstanceMapping BuildDictionaryMapping(
        MappingBuilderContext ctx,
        INewInstanceMapping keyMapping,
        INewInstanceMapping valueMapping
    )
    {
        if (TryGetFromEnumerable(ctx, keyMapping, valueMapping) is { } toDictionary)
            return toDictionary;

        // there might be an object factory for the exact types
        var hasObjectFactory = ctx.ObjectFactories.TryFindObjectFactory(ctx.Source, ctx.Target, out var objectFactory);

        // use generalized types to reuse generated mappings
        var sourceCollectionInfo = ctx.CollectionInfos!.Source;
        var targetType = ctx.Target;
        if (!hasObjectFactory)
        {
            sourceCollectionInfo = BuildCollectionTypeForSourceIDictionary(ctx);
            targetType = ctx
                .Types.Get(typeof(Dictionary<,>))
                .Construct(ctx.DictionaryInfos!.Target.Key, ctx.DictionaryInfos.Target.Value)
                .WithNullableAnnotation(NullableAnnotation.NotAnnotated);

            ctx.ObjectFactories.TryFindObjectFactory(sourceCollectionInfo.Type, targetType, out objectFactory);

            var delegateMapping = ctx.BuildDelegatedMapping(sourceCollectionInfo.Type, targetType);
            if (delegateMapping != null)
                return delegateMapping;
        }

        return new ForEachSetDictionaryMapping(
            sourceCollectionInfo.Type,
            targetType,
            keyMapping,
            valueMapping,
            ctx.CollectionInfos!.Source.CountIsKnown,
            objectFactory
        );
    }

    public static IExistingTargetMapping? TryBuildExistingTargetMapping(MappingBuilderContext ctx)
    {
        if (!ctx.IsConversionEnabled(MappingConversionType.Dictionary))
            return null;

        if (ctx.CollectionInfos == null)
            return null;

        if (!ctx.CollectionInfos.Target.ImplementedTypes.HasFlag(CollectionType.IDictionary))
            return null;

        if (BuildKeyValueMapping(ctx) is not var (keyMapping, valueMapping))
            return null;

        // if target is an immutable dictionary then don't create a foreach loop
        if (ctx.CollectionInfos.Target.ImplementedTypes.HasFlag(CollectionType.IImmutableDictionary))
        {
            ctx.ReportDiagnostic(DiagnosticDescriptors.CannotMapToReadOnlyMember);
            return null;
        }

        // add values to dictionary by setting key values in a foreach loop
        var ensureCapacityStatement = EnsureCapacityBuilder.TryBuildEnsureCapacity(ctx);

        return new ForEachSetDictionaryExistingTargetMapping(
            ctx.Source,
            ctx.Target,
            keyMapping,
            valueMapping,
            explicitCast: GetExplicitIndexer(ctx),
            ensureCapacity: ensureCapacityStatement
        );
    }

    private static (INewInstanceMapping, INewInstanceMapping)? BuildKeyValueMapping(MappingBuilderContext ctx)
    {
        var keyMapping = ctx.FindOrBuildMapping(ctx.DictionaryInfos!.Source.Key, ctx.DictionaryInfos.Target.Key);
        if (keyMapping == null)
            return null;

        var valueMapping = ctx.FindOrBuildMapping(ctx.DictionaryInfos.Source.Value, ctx.DictionaryInfos.Target.Value);
        if (valueMapping == null)
            return null;

        return (keyMapping, valueMapping);
    }

    private static INewInstanceMapping? TryGetFromEnumerable(
        MappingBuilderContext ctx,
        INewInstanceMapping keyMapping,
        INewInstanceMapping valueMapping
    )
    {
        if (!keyMapping.IsSynthetic || !valueMapping.IsSynthetic || keyMapping.TargetType.IsNullable())
            return null;

        // use .NET Core 2+ Dictionary constructor if value and key mapping is synthetic
        var enumerableType = ctx.Types.Get(typeof(IEnumerable<>));
        var dictionaryType = ctx.Types.Get(typeof(Dictionary<,>));

        var fromEnumerableCtor = dictionaryType.Constructors.FirstOrDefault(x =>
            x.Parameters.Length == 1
            && SymbolEqualityComparer.Default.Equals(((INamedTypeSymbol)x.Parameters[0].Type).ConstructedFrom, enumerableType)
        );

        if (fromEnumerableCtor != null)
        {
            var constructedDictionary = dictionaryType
                .Construct(keyMapping.TargetType, valueMapping.TargetType)
                .WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            return new CtorMapping(ctx.Source, constructedDictionary);
        }

        return null;
    }

    private static INamedTypeSymbol? GetExplicitIndexer(MappingBuilderContext ctx)
    {
        if (
            ctx.Target.ImplementsGeneric(
                ctx.Types.Get(typeof(IDictionary<,>)),
                SetterIndexerPropertyName,
                out var typedInter,
                out var isExplicit
            ) && !isExplicit
        )
            return null;

        return typedInter;
    }

    private static LinqDictionaryMapping? BuildImmutableMapping(
        MappingBuilderContext ctx,
        INewInstanceMapping keyMapping,
        INewInstanceMapping valueMapping
    )
    {
        return ctx.CollectionInfos!.Target.CollectionType switch
        {
            CollectionType.ImmutableSortedDictionary
                => new LinqDictionaryMapping(ctx.Source, ctx.Target, ToImmutableSortedDictionaryMethodName, keyMapping, valueMapping),
            CollectionType.ImmutableDictionary
            or CollectionType.IImmutableDictionary
                => new LinqDictionaryMapping(ctx.Source, ctx.Target, ToImmutableDictionaryMethodName, keyMapping, valueMapping),

            _ => null,
        };
    }

    private static CollectionInfo BuildCollectionTypeForSourceIDictionary(MappingBuilderContext ctx)
    {
        var info = ctx.CollectionInfos!.Source;

        // the types cannot be changed for mappings with a user symbol
        // as the types are defined by the user
        if (ctx.HasUserSymbol)
            return info;

        var dictionaryType = info.ImplementedTypes.HasFlag(CollectionType.IReadOnlyDictionary)
            ? BuildSourceDictionaryType(ctx, CollectionType.IReadOnlyDictionary)
            : info.ImplementedTypes.HasFlag(CollectionType.IDictionary)
                ? BuildSourceDictionaryType(ctx, CollectionType.IDictionary)
                : null;

        return dictionaryType == null
            ? info
            : CollectionInfoBuilder.BuildCollectionInfo(ctx.Types, ctx.SymbolAccessor, dictionaryType, info.EnumeratedType);
    }

    private static INamedTypeSymbol BuildSourceDictionaryType(MappingBuilderContext ctx, CollectionType type)
    {
        var genericType = CollectionInfoBuilder.GetGenericClrCollectionType(type);
        return (INamedTypeSymbol)
            ctx
                .Types.Get(genericType)
                .Construct(ctx.DictionaryInfos!.Source.Key, ctx.DictionaryInfos!.Source.Value)
                .WithNullableAnnotation(NullableAnnotation.NotAnnotated);
    }
}
