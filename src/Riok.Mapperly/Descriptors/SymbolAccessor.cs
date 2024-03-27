using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols;

namespace Riok.Mapperly.Descriptors;

public class SymbolAccessor(CompilationContext compilationContext, INamedTypeSymbol mapperSymbol)
{
    private readonly Dictionary<ISymbol, ImmutableArray<AttributeData>> _attributes = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, IReadOnlyCollection<ISymbol>> _allMembers = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, IReadOnlyCollection<IMappableMember>> _allAccessibleMembers =
        new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, IReadOnlyDictionary<string, IMappableMember>> _allAccessibleMembersCaseInsensitive =
        new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ITypeSymbol, IReadOnlyDictionary<string, IMappableMember>> _allAccessibleMembersCaseSensitive =
        new(SymbolEqualityComparer.Default);

    private MemberVisibility _memberVisibility = MemberVisibility.AllAccessible;

    private Compilation Compilation => compilationContext.Compilation;

    internal void SetMemberVisibility(MemberVisibility visibility) => _memberVisibility = visibility;

    public bool HasDirectlyAccessibleParameterlessConstructor(ITypeSymbol symbol) =>
        symbol is INamedTypeSymbol { IsAbstract: false } namedTypeSymbol
        && namedTypeSymbol.InstanceConstructors.Any(c => c.Parameters.IsDefaultOrEmpty && IsDirectlyAccessible(c));

    public bool IsDirectlyAccessible(ISymbol symbol) => Compilation.IsSymbolAccessibleWithin(symbol, mapperSymbol);

    public bool IsAccessible(ISymbol symbol)
    {
        if (_memberVisibility.HasFlag(MemberVisibility.Accessible) && !IsDirectlyAccessible(symbol))
            return false;

        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => _memberVisibility.HasFlag(MemberVisibility.Private),
            Accessibility.ProtectedAndInternal
                => _memberVisibility.HasFlag(MemberVisibility.Protected) && _memberVisibility.HasFlag(MemberVisibility.Internal),
            Accessibility.Protected => _memberVisibility.HasFlag(MemberVisibility.Protected),
            Accessibility.Internal => _memberVisibility.HasFlag(MemberVisibility.Internal),
            Accessibility.ProtectedOrInternal
                => _memberVisibility.HasFlag(MemberVisibility.Protected) || _memberVisibility.HasFlag(MemberVisibility.Internal),
            Accessibility.Public => _memberVisibility.HasFlag(MemberVisibility.Public),
            _ => false,
        };
    }

    public bool HasImplicitConversion(ITypeSymbol source, ITypeSymbol destination) =>
        Compilation.ClassifyConversion(source, destination).IsImplicit && (destination.IsNullable() || !source.IsNullable());

    public bool DoesTypeSatisfyTypeParameterConstraints(ITypeParameterSymbol typeParameter, ITypeSymbol type)
    {
        if (typeParameter.HasConstructorConstraint && !HasDirectlyAccessibleParameterlessConstructor(type))
            return false;

        if (!typeParameter.IsNullable() && type.IsNullable())
            return false;

        if (typeParameter.HasValueTypeConstraint && !type.IsValueType)
            return false;

        if (typeParameter.HasReferenceTypeConstraint && !type.IsReferenceType)
            return false;

        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            if (!Compilation.ClassifyConversion(type, UpgradeNullable(constraintType)).IsImplicit)
                return false;
        }

        return true;
    }

    public MethodParameter? WrapOptionalMethodParameter(IParameterSymbol? symbol)
    {
        return symbol == null ? null : WrapMethodParameter(symbol);
    }

    public MethodParameter WrapMethodParameter(IParameterSymbol symbol) => new(symbol, UpgradeNullable(symbol.Type));

    /// <summary>
    /// Upgrade the nullability of a symbol from <see cref="NullableAnnotation.None"/> to <see cref="NullableAnnotation.Annotated"/>.
    /// Does not upgrade the nullability of type parameters or array element types.
    /// </summary>
    /// <param name="symbol">The symbol to upgrade.</param>
    /// <returns>The upgraded symbol</returns>
    internal ITypeSymbol UpgradeNullable(ITypeSymbol symbol)
    {
        TryUpgradeNullable(symbol, out var upgradedSymbol);
        return upgradedSymbol ?? symbol;
    }

    /// <summary>
    /// Tries to upgrade the nullability of a symbol from <see cref="NullableAnnotation.None"/> to <see cref="NullableAnnotation.Annotated"/>.
    /// Value types are not upgraded.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <param name="upgradedSymbol">The upgraded symbol, if an upgrade has taken place, <c>null</c> otherwise.</param>
    /// <returns>Whether an upgrade has taken place.</returns>
    internal bool TryUpgradeNullable(ITypeSymbol symbol, [NotNullWhen(true)] out ITypeSymbol? upgradedSymbol)
    {
        if (symbol.NullableAnnotation != NullableAnnotation.None || symbol.IsValueType)
        {
            upgradedSymbol = default;
            return false;
        }

        switch (symbol)
        {
            case INamedTypeSymbol { TypeArguments.Length: > 0 } namedSymbol:
                var upgradedTypeArguments = namedSymbol.TypeArguments.Select(UpgradeNullable).ToImmutableArray();
                upgradedSymbol = namedSymbol
                    .ConstructedFrom.Construct(
                        upgradedTypeArguments,
                        upgradedTypeArguments.Select(ta => ta.NullableAnnotation).ToImmutableArray()
                    )
                    .WithNullableAnnotation(NullableAnnotation.Annotated);
                break;

            case IArrayTypeSymbol { ElementType.IsValueType: false, ElementNullableAnnotation: NullableAnnotation.None } arrayTypeSymbol:
                upgradedSymbol = compilationContext
                    .Compilation.CreateArrayTypeSymbol(
                        UpgradeNullable(arrayTypeSymbol.ElementType),
                        arrayTypeSymbol.Rank,
                        NullableAnnotation.Annotated
                    )
                    .WithNullableAnnotation(NullableAnnotation.Annotated);
                break;

            default:
                upgradedSymbol = symbol.WithNullableAnnotation(NullableAnnotation.Annotated);
                break;
        }

        return true;
    }

    internal IEnumerable<AttributeData> GetAttributes<T>(ISymbol symbol)
        where T : Attribute
    {
        var attributes = GetAttributesCore(symbol);
        if (attributes.IsEmpty)
        {
            yield break;
        }

        var attributeSymbol = compilationContext.Types.Get<T>();
        foreach (var attr in attributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.ConstructedFrom ?? attr.AttributeClass, attributeSymbol))
            {
                yield return attr;
            }
        }
    }

    internal static IEnumerable<AttributeData> GetAttributesSkipCache(ISymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.ConstructedFrom ?? attr.AttributeClass, attributeSymbol))
            {
                yield return attr;
            }
        }
    }

    internal bool HasAttribute<T>(ISymbol symbol)
        where T : Attribute => GetAttributes<T>(symbol).Any();

    internal IEnumerable<IMethodSymbol> GetAllMethods(ITypeSymbol symbol) => GetAllMembers(symbol).OfType<IMethodSymbol>();

    internal IEnumerable<IMethodSymbol> GetAllMethods(ITypeSymbol symbol, string name) =>
        GetAllMembers(symbol).Where(x => string.Equals(x.Name, name, StringComparison.Ordinal)).OfType<IMethodSymbol>();

    internal IEnumerable<IFieldSymbol> GetAllFields(ITypeSymbol symbol) => GetAllMembers(symbol).OfType<IFieldSymbol>();

    internal IReadOnlyCollection<ISymbol> GetAllMembers(ITypeSymbol symbol)
    {
        if (_allMembers.TryGetValue(symbol, out var members))
        {
            return members;
        }

        members = GetAllMembersCore(symbol).ToArray();
        _allMembers.Add(symbol, members);

        return members;
    }

    internal IReadOnlyCollection<IMappableMember> GetAllAccessibleMappableMembers(ITypeSymbol symbol)
    {
        if (_allAccessibleMembers.TryGetValue(symbol, out var members))
        {
            return members;
        }

        members = GetAllAccessibleMappableMembersCore(symbol).ToArray();
        _allAccessibleMembers.Add(symbol, members);

        return members;
    }

    internal bool TryFindMemberPath(
        ITypeSymbol type,
        IEnumerable<IEnumerable<string>> pathCandidates,
        IReadOnlyCollection<string> ignoredNames,
        bool ignoreCase,
        [NotNullWhen(true)] out MemberPath? memberPath
    )
    {
        var foundPath = new List<IMappableMember>();
        foreach (var pathCandidate in pathCandidates)
        {
            // reuse List instead of allocating a new one
            foundPath.Clear();
            if (!TryFindPath(type, pathCandidate, ignoreCase, foundPath))
                continue;

            if (ignoredNames.Contains(foundPath[0].Name))
                continue;

            memberPath = new(foundPath);
            return true;
        }

        memberPath = null;
        return false;
    }

    internal bool TryFindMemberPath(ITypeSymbol type, IReadOnlyCollection<string> path, [NotNullWhen(true)] out MemberPath? memberPath)
    {
        var foundPath = new List<IMappableMember>();
        if (TryFindPath(type, path, false, foundPath))
        {
            memberPath = new(foundPath);
            return true;
        }

        memberPath = null;
        return false;
    }

    private bool TryFindPath(ITypeSymbol type, IEnumerable<string> path, bool ignoreCase, ICollection<IMappableMember> foundPath)
    {
        foreach (var name in path)
        {
            // get T if type is Nullable<T>, prevents Value being treated as a member
            var actualType = type.NonNullableValueType() ?? type;
            if (GetMappableMember(actualType, name, ignoreCase) is not { } member)
                return false;

            type = member.Type;
            foundPath.Add(member);
        }

        return true;
    }

    private IMappableMember? GetMappableMember(ITypeSymbol symbol, string name, bool ignoreCase)
    {
        var membersBySymbol = ignoreCase ? _allAccessibleMembersCaseInsensitive : _allAccessibleMembersCaseSensitive;

        if (membersBySymbol.TryGetValue(symbol, out var symbolMembers))
            return symbolMembers.GetValueOrDefault(name);

        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        membersBySymbol[symbol] = symbolMembers = GetAllAccessibleMappableMembers(symbol)
            .GroupBy(x => x.Name, comparer)
            .ToDictionary(x => x.Key, x => x.First(), comparer);
        return symbolMembers.GetValueOrDefault(name);
    }

    private ImmutableArray<AttributeData> GetAttributesCore(ISymbol symbol)
    {
        if (_attributes.TryGetValue(symbol, out var attributes))
        {
            return attributes;
        }

        attributes = symbol.GetAttributes();
        _attributes.Add(symbol, attributes);

        return attributes;
    }

    private IEnumerable<ISymbol> GetAllMembersCore(ITypeSymbol symbol)
    {
        var members = symbol.GetMembers();

        if (symbol.TypeKind == TypeKind.Interface)
        {
            var interfaceProperties = symbol.AllInterfaces.SelectMany(GetAllMembers);
            return members.Concat(interfaceProperties);
        }

        return symbol.BaseType == null ? members : members.Concat(GetAllMembers(symbol.BaseType));
    }

    private IEnumerable<IMappableMember> GetAllAccessibleMappableMembersCore(ITypeSymbol symbol)
    {
        if (symbol.IsTupleType && symbol is INamedTypeSymbol namedType)
        {
            return namedType.TupleElements.Select(x => MappableMember.Create(this, x)).WhereNotNull();
        }

        // member must be property or a none backing variable field
        return GetAllMembers(symbol)
            .Where(x => x is { IsStatic: false, Kind: SymbolKind.Property } or IFieldSymbol { IsStatic: false, AssociatedSymbol: null })
            .DistinctBy(x => x.Name)
            .Select(x => MappableMember.Create(this, x))
            .WhereNotNull();
    }
}
