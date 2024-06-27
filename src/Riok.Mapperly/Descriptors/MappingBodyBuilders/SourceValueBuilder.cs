using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Configuration;
using Riok.Mapperly.Descriptors.MappingBodyBuilders.BuilderContext;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings.SourceValue;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Helpers;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.MappingBodyBuilders;

internal static class SourceValueBuilder
{
    /// <summary>
    /// Tries to build an <see cref="ISourceValue"/> instance which serializes as an expression,
    /// with a value that is assignable to the target member requested in the <paramref name="memberMappingInfo"/>.
    /// </summary>
    public static bool TryBuildMappedSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    ) => TryBuildMappedSourceValue(ctx, memberMappingInfo, MemberMappingBuilder.CodeStyle.Expression, out sourceValue);

    /// <summary>
    /// Tries to build an <see cref="ISourceValue"/> instance,
    /// with a value that is assignable to the target member requested in the <paramref name="memberMappingInfo"/>.
    /// </summary>
    public static bool TryBuildMappedSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        MemberMappingBuilder.CodeStyle codeStyle,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        if (memberMappingInfo.ValueConfiguration != null)
            return TryBuildValue(ctx, memberMappingInfo, out sourceValue);

        if (memberMappingInfo.SourceMember != null)
            return MemberMappingBuilder.TryBuild(ctx, memberMappingInfo, codeStyle, out sourceValue);

        sourceValue = null;
        return false;
    }

    private static bool TryBuildValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        // always set the member mapped,
        // as other diagnostics are reported if the mapping fails to be built
        ctx.SetMembersMapped(memberMappingInfo.TargetMember.Path[0].Name);

        if (memberMappingInfo.ValueConfiguration!.Value != null)
            return TryBuildConstantSourceValue(ctx, memberMappingInfo, out sourceValue);

        if (memberMappingInfo.ValueConfiguration!.Use != null)
            return TryBuildMethodProvidedSourceValue(ctx, memberMappingInfo, out sourceValue);

        throw new InvalidOperationException($"Illegal {nameof(MemberValueMappingConfiguration)}");
    }

    private static bool TryBuildConstantSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        var value = memberMappingInfo.ValueConfiguration!.Value!.Value;

        // the target is a non-nullable reference type,
        // but the provided value is null or default (for default IsNullable is also true)
        if (
            value.ConstantValue.IsNull
            && memberMappingInfo.TargetMember.MemberType.IsReferenceType
            && !memberMappingInfo.TargetMember.Member.IsNullable
        )
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.CannotMapValueNullToNonNullable,
                memberMappingInfo.TargetMember.ToDisplayString()
            );
            sourceValue = new ConstantSourceValue(SuppressNullableWarning(value.Expression));
            return true;
        }

        // target is value type but value is null
        if (
            value.ConstantValue.IsNull
            && memberMappingInfo.TargetMember.MemberType.IsValueType
            && !memberMappingInfo.TargetMember.MemberType.IsNullableValueType()
            && value.Expression.IsKind(SyntaxKind.NullLiteralExpression)
        )
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.CannotMapValueNullToNonNullable,
                memberMappingInfo.TargetMember.ToDisplayString()
            );
            sourceValue = new ConstantSourceValue(DefaultLiteral());
            return true;
        }

        // the target accepts null and the value is null or default
        // use the expression instant of a constant null literal
        // to use "default" or "null" depending on what the user specified in the attribute
        if (value.ConstantValue.IsNull)
        {
            sourceValue = new ConstantSourceValue(value.Expression);
            return true;
        }

        if (!SymbolEqualityComparer.Default.Equals(value.ConstantValue.Type, memberMappingInfo.TargetMember.MemberType))
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.MapValueTypeMismatch,
                value.Expression.ToFullString(),
                value.ConstantValue.Type?.ToDisplayString() ?? "unknown",
                memberMappingInfo.TargetMember.ToDisplayString()
            );
            sourceValue = null;
            return false;
        }

        switch (value.ConstantValue.Kind)
        {
            case TypedConstantKind.Primitive:
                sourceValue = new ConstantSourceValue(value.Expression);
                return true;
            case TypedConstantKind.Enum:
                // expand enum member access to fully qualified identifier
                // use simple member name approach instead of slower visitor pattern on the expression
                var enumMemberName = ((MemberAccessExpressionSyntax)value.Expression).Name.Identifier.Text;
                var enumTypeFullName = FullyQualifiedIdentifier(memberMappingInfo.TargetMember.MemberType);
                sourceValue = new ConstantSourceValue(MemberAccess(enumTypeFullName, enumMemberName));
                return true;
            case TypedConstantKind.Type:
            case TypedConstantKind.Array:
                ctx.BuilderContext.ReportDiagnostic(DiagnosticDescriptors.MapValueUnsupportedType, value.ConstantValue.Kind.ToString());
                break;
        }

        sourceValue = null;
        return false;
    }

    private static bool TryBuildMethodProvidedSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        if (ValidateValueProviderMethod(ctx, memberMappingInfo))
        {
            sourceValue = new MethodProvidedSourceValue(memberMappingInfo.ValueConfiguration!.Use!);
            return true;
        }

        sourceValue = null;
        return false;
    }

    private static bool ValidateValueProviderMethod(IMembersBuilderContext<IMapping> ctx, MemberMappingInfo memberMappingInfo)
    {
        var methodName = memberMappingInfo.ValueConfiguration!.Use!;
        var namedMethodCandidates = ctx
            .BuilderContext.MapperDeclaration.Symbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m is { IsAsync: false, ReturnsVoid: false, IsGenericMethod: false, Parameters.Length: 0 })
            .ToList();

        if (namedMethodCandidates.Count == 0)
        {
            ctx.BuilderContext.ReportDiagnostic(DiagnosticDescriptors.MapValueReferencedMethodNotFound, methodName);
            return false;
        }

        var methodCandidates = namedMethodCandidates.Where(x =>
            SymbolEqualityComparer.Default.Equals(x.ReturnType, memberMappingInfo.TargetMember.MemberType)
        );

        if (!memberMappingInfo.TargetMember.Member.IsNullable)
        {
            // only assume annotated is nullable, none is threated as non-nullable here
            methodCandidates = methodCandidates.Where(m => m.ReturnNullableAnnotation != NullableAnnotation.Annotated);
        }

        var method = methodCandidates.FirstOrDefault();
        if (method != null)
            return true;

        ctx.BuilderContext.ReportDiagnostic(
            DiagnosticDescriptors.MapValueMethodTypeMismatch,
            methodName,
            namedMethodCandidates[0].ReturnType.ToDisplayString(),
            memberMappingInfo.TargetMember.ToDisplayString()
        );
        return false;
    }
}
