# GreatUtilities.MapperWithPrimitiveOnly

An unofficial, Apache-2.0-licensed fork of [Mapperly](https://github.com/riok/mapperly), maintained at
[ziongh/mapperly](https://github.com/ziongh/mapperly/tree/primitive-only-maintained).
Upstream copyright and license notices remain unchanged. This fork adds opt-in `MapOnlyPrimitives` mapping.
The old `addPrimitiveOnly` branch is retained for historical reference, not used as the new base.

## Installation and use

Replace `Riok.Mapperly` with `GreatUtilities.MapperWithPrimitiveOnly`; **do not reference both packages**.
Assembly names, attributes and namespaces remain `Riok.Mapperly` for source compatibility.
This branch follows upstream `main`, currently the **5.0 prerelease** line, not stable Mapperly 4.3.1.
Use a prerelease package version until the corresponding upstream line is stable.

```csharp
using Riok.Mapperly.Abstractions;

[Mapper(MapOnlyPrimitives = true, UseDeepCloning = true)]
public static partial class EntityCopies
{
    public static partial Product CopyValues(Product source);
}
```

The option defaults to false, preserving upstream behavior. It can also be configured with
`[assembly: MapperDefaults(MapOnlyPrimitives = true)]` or the MSBuild property `MapperlyMapOnlyPrimitives`.
A mapper-level setting overrides the assembly/MSBuild default using upstream configuration precedence.

### Scalar policy

Automatically includes:

- Numeric/Boolean/character/string values, enums and nullable scalar values.
- Guid, DateTime, DateTimeOffset, DateOnly, TimeOnly, TimeSpan, Half, Int128 and UInt128.
- **Unmanaged value objects**, including strongly typed `int`, `long` and Guid entity keys and their nullable forms.
  They retain their original types and values; a private constructor/backing field does not prevent copying.
  The policy also works for keys from separately compiled assemblies.
- One-level arrays/enumerables of these scalar values and dictionaries with scalar keys and scalar values.

Automatically excludes object relationships, lists of entities, nested collections, `object`, arbitrary System
classes, and structs containing managed references such as lists. Unmanaged means the compiler proves there
are no managed references in the value; it is not a namespace/name heuristic. Reference-backed value objects
(such as string-backed wrappers) require explicit member configuration and, if needed, a user mapping.

Explicit member configurations take precedence over automatic exclusion. For example, to opt a string-backed
`Sku` into a mapping without reconstructing it:

```csharp
[MapProperty(nameof(Product.Sku), nameof(Product.Sku))]
public static partial Product CopyValues(Product source);
private static Sku CopySku(Sku value) => value;
```

This is an explicit escape hatch, not a security boundary: opting an entity relationship in can traverse it.
Required complex target members are not silently suppressed; normal required-member diagnostics remain.

### Copy semantics and limits

`MapOnlyPrimitives` controls **which members participate**, not cloning depth or allocation budgets.
`UseDeepCloning = true` copies included mutable collections; without it, normal upstream assignment rules apply.
Deep cloning does not apply to IQueryable projections. A scalar collection can still be large, and an arbitrary
IEnumerable can perform I/O or be unbounded. Filter/limit data before copying; this option provides no list-size,
SQL, cancellation, total-node or depth budget. Constructors/factories retain their normal upstream behavior.

Keys are **preserved by default**, including foreign keys. When creating a new database entity, explicitly
ignore/reset its primary key, uniqueness fields, concurrency tokens and audit fields as appropriate:

```csharp
[MapperIgnoreSource(nameof(Product.Id))]
[MapperIgnoreTarget(nameof(Product.Id))]
public static partial Product CopyForInsert(Product source);
```

Do not use generic copying to bypass domain validation or EF tracking/unit-of-work semantics.

## Maintaining the fork

Keep the delta small: the scalar classifier is in `PrimitiveMemberType.cs`, automatic exclusion hooks into
`IgnoredMembersBuilder`, and the remaining source changes only wire the option into upstream configuration.
Packaging, maintenance documentation and release tooling live under `fork/`; upstream package/project names
and release scripts are not rewritten.

Use **merge-based updates**, not force-pushing/rebasing published history:

```bash
git fetch upstream main --tags
git switch primitive-only-maintained
git merge --no-ff upstream/main
# Resolve any configuration/builder conflicts, then run the full validation below.
```

Retain upstream's `MapperAttribute`/configuration-merger conventions when adding options. Its reflection-based
configuration tests detect forgotten merge wiring. Before releasing, review changes in mapping builders,
required members, constructors, scalar conversions and supported Roslyn versions. Never auto-publish merely
because an upstream merge succeeds.

## Validation and packaging

```bash
dotnet tool restore
dotnet build /p:TreatWarningsAsErrors=true
dotnet test
python3 -m unittest discover -s fork -p 'test_*.py'
# Package every Roslyn version advertised by upstream's Roslyn*.props files:
RELEASE_VERSION=5.0.0-next.11.primitive.1 bash fork/package.sh
```

The isolated packaging overlay changes NuGet identity and readme only. The merger verifies shared lib/build
assets match and retains all version-specific analyzers. It also renames the build targets asset to match the
NuGet package ID, so MSBuild configuration and runtime attribute scope import correctly.

Before publishing, test the actual package from a local feed in clean consumers, including the oldest supported
SDK and the template's .NET 11 SDK. The fork CI automates package-consumer checks across .NET 8–11.
Run `python3 fork/smoke-package.py PACKAGE.nupkg net10.0 10.0.400` for a clean, SDK-selected consumer.
Verify typed/nullable keys, ignored navigation getters, independent scalar
lists, required-member failure and the MSBuild option. Run the NuGet validator. Keep upstream integration
coverage; its package-based path assumes `Riok.Mapperly`, so fork package consumers are tested separately.

Publishing is intentionally separate from packaging and must use a reviewed, committed/pushed source revision:

```bash
RELEASE_VERSION=5.0.0-next.11.primitive.1 bash fork/publish.sh
```

The publish script refuses dirty/unpushed source, checks the package repository commit, and runs the full NuGet
validator (including SourceLink) before pushing. It does not publish to an arbitrary configured feed.

Never commit API keys. NuGet versions are immutable. Choose a new version for each release, and preserve
upstream prerelease status in the fork's version. A release must not imply that this fork is the official Mapperly.
