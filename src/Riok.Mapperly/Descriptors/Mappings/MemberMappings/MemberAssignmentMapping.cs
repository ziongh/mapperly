using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings.SourceValue;
using Riok.Mapperly.Symbols;

namespace Riok.Mapperly.Descriptors.Mappings.MemberMappings;

/// <summary>
/// Represents a member mapping including an assignment to a target member.
/// (e.g. target.A = source.B or target.A = "fooBar")
/// </summary>
[DebuggerDisplay("MemberAssignmentMapping({_sourceValue} => {_targetPath})")]
public class MemberAssignmentMapping(SetterMemberPath targetPath, ISourceValue sourceValue, MemberMappingInfo memberInfo)
    : IMemberAssignmentMapping
{
    public MemberMappingInfo MemberInfo { get; } = memberInfo;

    private readonly ISourceValue _sourceValue = sourceValue;
    private readonly SetterMemberPath _targetPath = targetPath;

    public IEnumerable<StatementSyntax> Build(TypeMappingBuildContext ctx, ExpressionSyntax targetAccess) =>
        ctx.SyntaxFactory.SingleStatement(BuildExpression(ctx, targetAccess));

    public ExpressionSyntax BuildExpression(TypeMappingBuildContext ctx, ExpressionSyntax? targetAccess)
    {
        var mappedValue = _sourceValue.Build(ctx);

        // target.SetValue(source.Value); or target.Value = source.Value;
        return _targetPath.BuildAssignment(targetAccess, mappedValue);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((MemberAssignmentMapping)obj);
    }

    public override int GetHashCode() => HashCode.Combine(_sourceValue, _targetPath);

    public static bool operator ==(MemberAssignmentMapping? left, MemberAssignmentMapping? right) => Equals(left, right);

    public static bool operator !=(MemberAssignmentMapping? left, MemberAssignmentMapping? right) => !Equals(left, right);

    protected bool Equals(MemberAssignmentMapping other)
    {
        return _sourceValue.Equals(other._sourceValue) && _targetPath.Equals(other._targetPath);
    }
}
