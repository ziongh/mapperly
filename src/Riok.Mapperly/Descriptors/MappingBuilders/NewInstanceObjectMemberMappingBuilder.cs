using Microsoft.CodeAnalysis;
using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.ExistingTarget;
using Riok.Mapperly.Helpers;

namespace Riok.Mapperly.Descriptors.MappingBuilders;

public static class NewInstanceObjectMemberMappingBuilder
{
    public static INewInstanceMapping? TryBuildMapping(MappingBuilderContext ctx)
    {
        if (ctx.Target.SpecialType != SpecialType.None || ctx.Source.SpecialType != SpecialType.None)
            return null;

        if (ctx.Source.IsDelegate() || ctx.Target.IsDelegate())
            return null;

        if (ctx.ObjectFactories.TryFindObjectFactory(ctx.Source, ctx.Target, out var objectFactory))
            return new NewInstanceObjectFactoryMemberMapping(
                ctx.Source,
                ctx.Target.NonNullable(),
                objectFactory,
                ctx.Configuration.Mapper.UseReferenceHandling
            );

        if (
            ctx.Target is not INamedTypeSymbol namedTarget
            || namedTarget.Constructors.All(x => !ctx.SymbolAccessor.IsDirectlyAccessible(x))
        )
            return null;

        if (ctx.Source.IsEnum() || ctx.Target.IsEnum())
            return null;

        if (ctx.Target.IsTupleType)
        {
            if (!ctx.IsConversionEnabled(MappingConversionType.Tuple))
                return null;

            // inline expressions don't support tuple expressions so ValueTuple is used instead
            if (ctx.IsExpression)
            {
                return new NewValueTupleConstructorMapping(ctx.Source, ctx.Target);
            }

            var expectedArgumentCount = (ctx.Target as INamedTypeSymbol)!.TupleElements.Length;
            return new NewValueTupleExpressionMapping(ctx.Source, ctx.Target, expectedArgumentCount);
        }

        // inline expressions don't support method property mappings
        // and can only map to properties via object initializers.
        return ctx.IsExpression
            ? new NewInstanceObjectMemberMapping(ctx.Source, ctx.Target.NonNullable())
            : new NewInstanceObjectMemberMethodMapping(ctx.Source, ctx.Target.NonNullable(), ctx.Configuration.Mapper.UseReferenceHandling);
    }

    public static IExistingTargetMapping? TryBuildExistingTargetMapping(MappingBuilderContext ctx)
    {
        if (ctx.Target.SpecialType != SpecialType.None || ctx.Source.SpecialType != SpecialType.None)
            return null;

        if (ctx.Source.IsEnum() || ctx.Target.IsEnum())
            return null;

        return new ObjectMemberExistingTargetMapping(ctx.Source, ctx.Target);
    }
}
