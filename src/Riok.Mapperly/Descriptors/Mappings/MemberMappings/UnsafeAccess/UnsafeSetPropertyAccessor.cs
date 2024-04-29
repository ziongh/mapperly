using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Emit;
using Riok.Mapperly.Emit.Syntax;
using Riok.Mapperly.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings.MemberMappings.UnsafeAccess;

/// <summary>
/// Creates an extension method to set an objects non public property using .Net 8's UnsafeAccessor.
/// /// <code>
/// [UnsafeAccessor(UnsafeAccessorKind.Property, Name = "set_value")]
/// public extern static void SetValue(this global::MyClass source, int value);
/// </code>
/// </summary>
public class UnsafeSetPropertyAccessor(IPropertySymbol value, string methodName) : IUnsafeAccessor
{
    private const string DefaultTargetParameterName = "target";
    private const string DefaultValueParameterName = "value";

    private readonly string _targetType = value.ContainingType.FullyQualifiedIdentifierName();
    private readonly string _valueType = value.Type.FullyQualifiedIdentifierName();
    private readonly string _memberName = value.Name;

    public string MethodName { get; } = methodName;

    public MethodDeclarationSyntax BuildMethod(SourceEmitterContext ctx)
    {
        var nameBuilder = ctx.NameBuilder.NewScope();
        var targetName = nameBuilder.New(DefaultTargetParameterName);
        var valueName = nameBuilder.New(DefaultValueParameterName);

        var target = Parameter(_targetType, targetName, true);
        var targetValue = Parameter(_valueType, valueName);

        var parameters = ParameterList(CommaSeparatedList(target, targetValue));
        var attributeList = ctx.SyntaxFactory.UnsafeAccessorAttributeList(UnsafeAccessorType.Method, $"set_{_memberName}");

        return PublicStaticExternMethod(
            ctx,
            PredefinedType(Token(SyntaxKind.VoidKeyword)).AddTrailingSpace(),
            MethodName,
            parameters,
            attributeList
        );
    }
}
