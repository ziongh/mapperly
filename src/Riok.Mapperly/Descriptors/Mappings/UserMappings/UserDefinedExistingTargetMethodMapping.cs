using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Abstractions.ReferenceHandling;
using Riok.Mapperly.Descriptors.Mappings.ExistingTarget;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings.UserMappings;

/// <summary>
/// Represents a mapping method declared but not implemented by the user which reuses an existing target object instance.
/// </summary>
public class UserDefinedExistingTargetMethodMapping(
    IMethodSymbol method,
    MethodParameter sourceParameter,
    MethodParameter targetParameter,
    MethodParameter? referenceHandlerParameter,
    bool enableReferenceHandling
) : MethodMapping(method, sourceParameter, referenceHandlerParameter, targetParameter.Type), IExistingTargetUserMapping
{
    private IExistingTargetMapping? _delegateMapping;

    public IMethodSymbol Method { get; } = method;

    public bool? Default => false;

    public bool IsExternal => false;

    private MethodParameter TargetParameter { get; } = targetParameter;

    /// <summary>
    /// The reference handling is enabled but is only internal to this method.
    /// No reference handler parameter is passed.
    /// </summary>
    private bool InternalReferenceHandlingEnabled => enableReferenceHandling && ReferenceHandlerParameter == null;

    public void SetDelegateMapping(IExistingTargetMapping delegateMapping) => _delegateMapping = delegateMapping;

    public override ExpressionSyntax Build(TypeMappingBuildContext ctx) =>
        throw new InvalidOperationException($"{nameof(UserDefinedExistingTargetMethodMapping)} does not support {nameof(Build)}");

    public IEnumerable<StatementSyntax> Build(TypeMappingBuildContext ctx, ExpressionSyntax target)
    {
        return ctx.SyntaxFactory.SingleStatement(
            Invocation(
                MethodName,
                SourceParameter.WithArgument(ctx.Source),
                TargetParameter.WithArgument(target),
                ReferenceHandlerParameter?.WithArgument(ctx.ReferenceHandler)
            )
        );
    }

    public override IEnumerable<StatementSyntax> BuildBody(TypeMappingBuildContext ctx)
    {
        if (_delegateMapping == null)
        {
            yield return ThrowStatement(ctx.SyntaxFactory.ThrowMappingNotImplementedExceptionStatement());
            yield break;
        }

        // if the source type is nullable, add a null guard.
        // if (source == null || target == null)
        //    return;
        if (SourceType.IsNullable() || TargetType.IsNullable())
        {
            yield return BuildNullGuard(ctx);
        }

        // if reference handling is enabled and no reference handler parameter is declared
        // a new reference handler is instantiated and used.
        if (InternalReferenceHandlingEnabled)
        {
            // var refHandler = new RefHandler();
            var referenceHandlerName = ctx.NameBuilder.New(DefaultReferenceHandlerParameterName);
            var createRefHandler = ctx.SyntaxFactory.CreateInstance<PreserveReferenceHandler>();
            yield return ctx.SyntaxFactory.DeclareLocalVariable(referenceHandlerName, createRefHandler);
            ctx = ctx.WithRefHandler(referenceHandlerName);
        }

        foreach (var statement in _delegateMapping.Build(ctx, IdentifierName(TargetParameter.Name)))
        {
            yield return statement;
        }
    }

    protected override ParameterListSyntax BuildParameterList()
        // needs to include the target parameter
        =>
        ParameterList(IsExtensionMethod, SourceParameter, TargetParameter, ReferenceHandlerParameter);

    internal override void EnableReferenceHandling(INamedTypeSymbol iReferenceHandlerType)
    {
        // the parameters of user defined methods should not be manipulated
    }

    private StatementSyntax BuildNullGuard(TypeMappingBuildContext ctx)
    {
        var condition = IfAnyNull((SourceType, ctx.Source), (TargetType, IdentifierName(TargetParameter.Name)));
        return ctx.SyntaxFactory.If(condition, ctx.SyntaxFactory.AddIndentation().Return());
    }
}
