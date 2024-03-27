using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Helpers;

namespace Riok.Mapperly.Descriptors.MappingBuilders;

public static class ExplicitCastMappingBuilder
{
    public static CastMapping? TryBuildMapping(MappingBuilderContext ctx)
    {
        if (!ctx.IsConversionEnabled(MappingConversionType.ExplicitCast))
            return null;

        if (ctx.Configuration.Mapper.UseDeepCloning && !ctx.Source.IsImmutable() && !ctx.Target.IsImmutable())
            return null;

        // ClassifyConversion does not check if tuple field member names are the same
        // if tuple check isn't done then (A: int, B: int) -> (B: int, A: int) would be mapped
        // return ((int, int))source; instead of return (B: source.A, A: source.B);
        if (ctx.Target.IsTupleType)
            return null;

        if (SymbolEqualityComparer.Default.Equals(ctx.Source, ctx.Compilation.ObjectType))
            return null;

        var conversion = ctx.Compilation.ClassifyConversion(ctx.Source, ctx.Target);

        // only allow user defined explicit reference conversions
        // since other may return an extra runtime type check or may throw InvalidCastException.
        // see c# language specification section 10.3.5 explicit reference conversions
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#1035-explicit-reference-conversions
        return conversion.IsExplicit && (!conversion.IsReference || conversion.IsUserDefined)
            ? new CastMapping(ctx.Source, ctx.Target)
            : null;
    }
}
