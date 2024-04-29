using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings.MemberMappings;

/// <summary>
/// Represents a null safe <see cref="IMemberMapping"/>.
/// (eg. <c>source?.A?.B ?? null-substitute</c> or <c>source?.A?.B != null ? MapToD(source.A.B) : null-substitute</c>)
/// </summary>
[DebuggerDisplay("NullMemberMapping({SourcePath}: {_delegateMapping})")]
public class NullMemberMapping(
    INewInstanceMapping delegateMapping,
    GetterMemberPath sourcePath,
    ITypeSymbol targetType,
    NullFallbackValue nullFallback,
    bool useNullConditionalAccess
) : IMemberMapping
{
    private readonly INewInstanceMapping _delegateMapping = delegateMapping;
    private readonly NullFallbackValue _nullFallback = nullFallback;

    public GetterMemberPath SourcePath { get; } = sourcePath;

    public ExpressionSyntax Build(TypeMappingBuildContext ctx)
    {
        // the source type of the delegate mapping is nullable or the source path is not nullable
        // build mapping with null conditional access
        if (_delegateMapping.SourceType.IsNullable() || !SourcePath.IsAnyNullable())
        {
            ctx = ctx.WithSource(SourcePath.BuildAccess(ctx.Source, nullConditional: true));
            return _delegateMapping.Build(ctx);
        }

        // source is nullable and the mapping method cannot handle nulls,
        // call mapping only if source is not null.
        // source.A?.B == null ? <null-substitute> : Map(source.A.B)
        // or for nullable value types:
        // source.A?.B == null ? <null-substitute> : Map(source.A.B.Value)
        // use simplified coalesce expression for synthetic mappings:
        // source.A?.B ?? <null-substitute>
        if (_delegateMapping.IsSynthetic && (useNullConditionalAccess || !SourcePath.IsAnyObjectPathNullable()))
        {
            var nullConditionalSourceAccess = SourcePath.BuildAccess(ctx.Source, nullConditional: true);
            var nameofSourceAccess = SourcePath.BuildAccess(ctx.Source, nullConditional: false);
            var mapping = _delegateMapping.Build(ctx.WithSource(nullConditionalSourceAccess));
            return _nullFallback == NullFallbackValue.Default && targetType.IsNullable()
                ? mapping
                : Coalesce(mapping, NullSubstitute(targetType, nameofSourceAccess, _nullFallback));
        }

        var notNullCondition = useNullConditionalAccess
            ? IsNotNull(SourcePath.BuildAccess(ctx.Source, nullConditional: true, skipTrailingNonNullable: true))
            : SourcePath.BuildNonNullConditionWithoutConditionalAccess(ctx.Source)!;
        var sourceMemberAccess = SourcePath.BuildAccess(ctx.Source, true);
        ctx = ctx.WithSource(sourceMemberAccess);
        return Conditional(notNullCondition, _delegateMapping.Build(ctx), NullSubstitute(targetType, sourceMemberAccess, _nullFallback));
    }

    protected bool Equals(NullMemberMapping other) =>
        _delegateMapping.Equals(other._delegateMapping) && _nullFallback == other._nullFallback && SourcePath.Equals(other.SourcePath);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((NullMemberMapping)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _delegateMapping.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)_nullFallback;
            hashCode = (hashCode * 397) ^ SourcePath.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(NullMemberMapping? left, NullMemberMapping? right) => Equals(left, right);

    public static bool operator !=(NullMemberMapping? left, NullMemberMapping? right) => !Equals(left, right);
}
