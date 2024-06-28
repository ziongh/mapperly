using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Helpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Riok.Mapperly.Emit.Syntax;

public partial struct SyntaxFactoryHelper
{
    public static AssignmentExpressionSyntax CoalesceAssignment(ExpressionSyntax target, ExpressionSyntax source)
    {
        return AssignmentExpression(
            SyntaxKind.CoalesceAssignmentExpression,
            target,
            SpacedToken(SyntaxKind.QuestionQuestionEqualsToken),
            source
        );
    }

    public static SyntaxTrivia Nullable(bool enabled)
    {
        return Trivia(NullableDirectiveTrivia(LeadingSpacedToken(enabled ? SyntaxKind.EnableKeyword : SyntaxKind.DisableKeyword), true));
    }

    public static BinaryExpressionSyntax Coalesce(ExpressionSyntax expr, ExpressionSyntax coalesceExpr) =>
        BinaryExpression(SyntaxKind.CoalesceExpression, expr, coalesceExpr);

    public static IdentifierNameSyntax NonNullableIdentifier(ITypeSymbol t) => FullyQualifiedIdentifier(t.NonNullable());

    public static ExpressionSyntax NullSubstitute(ITypeSymbol t, ExpressionSyntax argument, NullFallbackValue nullFallbackValue)
    {
        return nullFallbackValue switch
        {
            NullFallbackValue.Default when t.IsNullableValueType() => DefaultExpression(FullyQualifiedIdentifier(t)),
            NullFallbackValue.Default => DefaultLiteral(),
            NullFallbackValue.EmptyString => StringLiteral(string.Empty),
            NullFallbackValue.CreateInstance => CreateInstance(t),
            _ when argument is ElementAccessExpressionSyntax memberAccess
                => ThrowNullReferenceException(
                    InterpolatedString(
                        $"Sequence {NameOf(memberAccess.Expression)}, contained a null value at index {memberAccess.ArgumentList.Arguments[0].Expression}."
                    )
                ),
            _ when argument is MemberAccessExpressionSyntax or SimpleNameSyntax => ThrowArgumentNullException(argument),
            _ when argument is InvocationExpressionSyntax invocation
                => ThrowNullReferenceException(StringLiteral(invocation.Expression + " returned null")),
            _ => ThrowNullReferenceException(StringLiteral(argument + " is null")),
        };
    }

    public StatementSyntax IfNullReturnOrThrow(ExpressionSyntax expression, ExpressionSyntax? returnOrThrowExpression = null)
    {
        StatementSyntax ifExpression = returnOrThrowExpression switch
        {
            ThrowExpressionSyntax throwSyntax => AddIndentation().ThrowStatement(throwSyntax.Expression),
            _ => AddIndentation().Return(returnOrThrowExpression),
        };

        return If(IsNull(expression), ifExpression);
    }

    public static ExpressionSyntax SuppressNullableWarning(ExpressionSyntax expression) =>
        PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, expression);
}
