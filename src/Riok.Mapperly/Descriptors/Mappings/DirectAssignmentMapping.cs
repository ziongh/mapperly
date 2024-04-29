using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Riok.Mapperly.Descriptors.Mappings;

/// <summary>
/// Represents a direct assignment mapping.
/// Source and target types need to be the same types.
/// </summary>
public class DirectAssignmentMapping(ITypeSymbol type) : NewInstanceMapping(type, type)
{
    public override ExpressionSyntax Build(TypeMappingBuildContext ctx) => ctx.Source;

    public override bool IsSynthetic => true;
}
