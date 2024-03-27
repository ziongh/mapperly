using Microsoft.CodeAnalysis;

namespace Riok.Mapperly.Descriptors.Mappings;

/// <summary>
/// Represents a mapping from one type to another.
/// </summary>
public interface IMapping
{
    ITypeSymbol SourceType { get; }

    ITypeSymbol TargetType { get; }
}
