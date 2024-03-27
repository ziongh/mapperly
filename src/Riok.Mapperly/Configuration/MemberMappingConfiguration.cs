using Riok.Mapperly.Descriptors;

namespace Riok.Mapperly.Configuration;

public record MemberMappingConfiguration(StringMemberPath Source, StringMemberPath Target) : HasSyntaxReference
{
    public string? StringFormat { get; set; }

    public string? FormatProvider { get; set; }

    public string? Use { get; set; }

    public bool IsValid => Use == null || FormatProvider == null && StringFormat == null;

    public TypeMappingConfiguration ToTypeMappingConfiguration() => new(StringFormat, FormatProvider, Use);
}
