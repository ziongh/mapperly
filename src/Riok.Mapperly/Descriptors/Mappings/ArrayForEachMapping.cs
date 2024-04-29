using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings;

/// <summary>
/// Represents an enumerable to array mapping which works by initialising an array, looping through the source,
/// mapping each element and adding it to the target array.
/// </summary>
public class ArrayForEachMapping(
    ITypeSymbol sourceType,
    ITypeSymbol targetType,
    INewInstanceMapping elementMapping,
    ITypeSymbol targetArrayElementType,
    string countPropertyName
) : NewInstanceMethodMapping(sourceType, targetType)
{
    private const string TargetVariableName = "target";
    private const string LoopItemVariableName = "item";
    private const string LoopCounterName = "i";

    public override IEnumerable<StatementSyntax> BuildBody(TypeMappingBuildContext ctx)
    {
        var targetVariableName = ctx.NameBuilder.New(TargetVariableName);
        var loopCounterVariableName = ctx.NameBuilder.New(LoopCounterName);

        // var target = new T[source.Count];
        var sourceLengthArrayRank = ArrayRankSpecifier(
            SingletonSeparatedList<ExpressionSyntax>(MemberAccess(ctx.Source, countPropertyName))
        );
        var targetInitializationValue = CreateArray(
            ArrayType(FullyQualifiedIdentifier(targetArrayElementType)).WithRankSpecifiers(SingletonList(sourceLengthArrayRank))
        );
        yield return ctx.SyntaxFactory.DeclareLocalVariable(targetVariableName, targetInitializationValue);

        // var i = 0;
        yield return ctx.SyntaxFactory.DeclareLocalVariable(loopCounterVariableName, IntLiteral(0));

        // target[i] = Map(item);
        var (loopItemCtx, loopItemVariableName) = ctx.WithNewSource(LoopItemVariableName);
        var convertedSourceItemExpression = elementMapping.Build(loopItemCtx.AddIndentation());

        var assignment = Assignment(
            ElementAccess(IdentifierName(targetVariableName), IdentifierName(loopCounterVariableName)),
            convertedSourceItemExpression
        );

        // i++;
        var counterIncrement = PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, IdentifierName(loopCounterVariableName));

        // foreach(var item in source)
        //{
        //   target[i] = Map(item);
        //   i++;
        //}
        yield return ctx.SyntaxFactory.ForEach(loopItemVariableName, ctx.Source, assignment, counterIncrement);

        // return target;
        yield return ctx.SyntaxFactory.ReturnVariable(targetVariableName);
    }
}
