using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings.MemberMappings;

public class ConstructorParameterMapping(
    IParameterSymbol parameter,
    NullMemberMapping delegateMapping,
    bool selfOrPreviousIsUnmappedOptional
)
{
    private readonly bool _selfOrPreviousIsUnmappedOptional = selfOrPreviousIsUnmappedOptional;

    /// <summary>
    /// The parameter of the constructor.
    /// Note: the nullability of it may not be "upgraded".
    /// </summary>
    public IParameterSymbol Parameter { get; } = parameter;

    public NullMemberMapping DelegateMapping { get; } = delegateMapping;

    public ArgumentSyntax BuildArgument(TypeMappingBuildContext ctx)
    {
        var argumentExpression = DelegateMapping.Build(ctx);
        var arg = Argument(argumentExpression);
        return _selfOrPreviousIsUnmappedOptional ? arg.WithNameColon(SpacedNameColon(Parameter.Name)) : arg;
    }

    protected bool Equals(ConstructorParameterMapping other) =>
        Parameter.Equals(other.Parameter, SymbolEqualityComparer.Default)
        && DelegateMapping.Equals(other.DelegateMapping)
        && _selfOrPreviousIsUnmappedOptional == other._selfOrPreviousIsUnmappedOptional;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((ConstructorParameterMapping)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(Parameter);
            hashCode = (hashCode * 397) ^ DelegateMapping.GetHashCode();
            hashCode = (hashCode * 397) ^ _selfOrPreviousIsUnmappedOptional.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ConstructorParameterMapping? left, ConstructorParameterMapping? right) => Equals(left, right);

    public static bool operator !=(ConstructorParameterMapping? left, ConstructorParameterMapping? right) => !Equals(left, right);
}
