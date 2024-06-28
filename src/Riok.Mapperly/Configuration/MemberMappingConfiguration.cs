using System.Diagnostics;
using Riok.Mapperly.Descriptors;

namespace Riok.Mapperly.Configuration;

[DebuggerDisplay("{Source} => {Target}")]
public record MemberMappingConfiguration(StringMemberPath Source, StringMemberPath Target) : HasSyntaxReference
{
    /// <summary>
    /// Used to adapt from <see cref="Abstractions.MapPropertyFromSourceAttribute"/>
    /// </summary>
    public MemberMappingConfiguration(StringMemberPath Target)
        : this(Source: StringMemberPath.Empty, Target) { }

    public string? StringFormat { get; set; }

    public string? FormatProvider { get; set; }

    public string? Use { get; set; }

    public bool IsValid => Use == null || FormatProvider == null && StringFormat == null;

    public TypeMappingConfiguration ToTypeMappingConfiguration() => new(StringFormat, FormatProvider, Use);
}
