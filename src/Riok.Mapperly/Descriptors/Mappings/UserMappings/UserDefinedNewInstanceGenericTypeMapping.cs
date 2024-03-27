using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Emit;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings.UserMappings;

/// <summary>
/// A user defined generic mapping method,
/// which can have a generic source parameter,
/// a generic target parameter or both.
/// Similar to <see cref="UserDefinedNewInstanceRuntimeTargetTypeParameterMapping"/>.
/// </summary>
public class UserDefinedNewInstanceGenericTypeMapping(
    IMethodSymbol method,
    GenericMappingTypeParameters typeParameters,
    MappingMethodParameters parameters,
    ITypeSymbol targetType,
    bool enableReferenceHandling,
    NullFallbackValue nullArm,
    ITypeSymbol objectType
)
    : UserDefinedNewInstanceRuntimeTargetTypeMapping(
        method,
        parameters.Source,
        parameters.ReferenceHandler,
        targetType,
        enableReferenceHandling,
        nullArm,
        objectType
    )
{
    public GenericMappingTypeParameters TypeParameters { get; } = typeParameters;

    public override MethodDeclarationSyntax BuildMethod(SourceEmitterContext ctx) =>
        base.BuildMethod(ctx).WithTypeParameterList(TypeParameterList(TypeParameters.SourceType, TypeParameters.TargetType));

    protected override ExpressionSyntax BuildTargetType()
    {
        // typeof(TTarget) or typeof(<ReturnType>)
        var targetTypeName = TypeParameters.TargetType ?? TargetType;
        return TypeOfExpression(FullyQualifiedIdentifier(targetTypeName.NonNullable()));
    }

    protected override ExpressionSyntax? BuildSwitchArmWhenClause(ExpressionSyntax targetType, RuntimeTargetTypeMapping mapping)
    {
        return mapping.IsAssignableToMethodTargetType ? null : base.BuildSwitchArmWhenClause(targetType, mapping);
    }
}
