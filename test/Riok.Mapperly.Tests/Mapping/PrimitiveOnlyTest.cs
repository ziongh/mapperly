using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Diagnostics;

namespace Riok.Mapperly.Tests.Mapping;

public class PrimitiveOnlyTest
{
    private static readonly TestSourceBuilderOptions Options = new(UseDeepCloning: true, MapOnlyPrimitives: true);

    [Theory]
    [InlineData("int")]
    [InlineData("int?")]
    [InlineData("decimal")]
    [InlineData("string")]
    [InlineData("Guid")]
    [InlineData("Guid?")]
    [InlineData("DateTime")]
    [InlineData("DateTimeOffset?")]
    [InlineData("DateOnly")]
    [InlineData("TimeOnly")]
    [InlineData("TimeSpan")]
    [InlineData("E")]
    [InlineData("E?")]
    public void CopiesScalarButNotRelations(string scalar)
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "A",
            Options,
            $$"""
            class A
            {
                public {{scalar}} Value { get; set; }
                public A Parent { get; set; }
                public List<A> Children { get; set; }
                public Dictionary<string, A> Index { get; set; }
                public Exception Error { get; set; }
                public object Payload { get; set; }
            }
            enum E { First, Second }
            """
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::A();
                target.Value = source.Value;
                return target;
                """
            );
    }

    [Theory]
    [InlineData("List<string>")]
    [InlineData("int[]")]
    [InlineData("List<int?>")]
    [InlineData("Dictionary<string, int>")]
    [InlineData("IReadOnlyDictionary<string, Guid>")]
    public void IncludesScalarCollections(string collection)
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            new TestSourceBuilderOptions(MapOnlyPrimitives: true),
            $$"""
            class A { public {{collection}} Values { get; set; } }
            class B { public {{collection}} Values { get; set; } }
            """
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B();
                target.Values = source.Values;
                return target;
                """
            );
    }

    [Theory]
    [InlineData("int[][]")]
    [InlineData("List<List<int>>")]
    [InlineData("Dictionary<string, List<int>>")]
    [InlineData("List<A>")]
    [InlineData("MutableContainer")]
    public void ExcludesNestedCollectionsAndCustomTypes(string complex)
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "A",
            Options,
            $$"""
            class A { public int Value { get; set; } public {{complex}} Excluded { get; set; } }
            readonly record struct MutableContainer(List<int> Values);
            """
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::A();
                target.Value = source.Value;
                return target;
                """
            );
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("Guid")]
    public void CopiesStronglyTypedKeysWithoutUnwrapping(string underlying)
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "A",
            Options,
            $$"""
            class A
            {
                public Key Id { get; set; }
                public Key? OwnerId { get; set; }
                public A Parent { get; set; }
            }
            readonly struct Key
            {
                private readonly {{underlying}} _value;
                private Key({{underlying}} value) => _value = value;
                public {{underlying}} Value => _value;
                public static Key From({{underlying}} value) => new Key(value);
            }
            """
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::A();
                target.Id = source.Id;
                target.OwnerId = source.OwnerId;
                return target;
                """
            );
    }

    [Fact]
    public void CopiesKeyFromCompiledAssemblyWithPrivateBackingField()
    {
        var keys = TestHelper.BuildAssembly(
            "TypedKeys",
            TestSourceBuilder.SyntaxTree(
                "public readonly struct EntityId { private readonly long _value; private EntityId(long value) => _value = value; public long Value => _value; public static EntityId From(long value) => new EntityId(value); }"
            )
        );
        var source = TestSourceBuilder.Mapping(
            "A",
            "A",
            Options,
            "class A { public EntityId Id { get; set; } public EntityId? OwnerId { get; set; } public A Parent { get; set; } }"
        );
        TestHelper
            .GenerateMapper(source, additionalAssemblies: [keys])
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::A();
                target.Id = source.Id;
                target.OwnerId = source.OwnerId;
                return target;
                """
            );
    }

    [Fact]
    public void ExplicitConfigurationOptsCustomValueTypeIn()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            [MapProperty(nameof(A.Id), nameof(A.Id))]
            private partial A Map(A source);
            private static Key CopyKey(Key key) => key;
            """,
            Options,
            "class A { public Key Id { get; set; } public A Parent { get; set; } }",
            "readonly record struct Key(string Value);"
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::A();
                target.Id = CopyKey(source.Id);
                return target;
                """
            );
    }

    [Fact]
    public void RequiredComplexMemberIsNotSilentlyDiscarded()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            Options,
            "class A { public int Value { get; set; } public A Child { get; set; } }",
            "class B { public int Value { get; set; } public required A Child { get; init; } }"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(
                DiagnosticDescriptors.RequiredMemberNotMapped,
                "Required member Child on mapping target type B was not found on the mapping source type A"
            )
            .HaveAssertedAllDiagnostics();
    }

    [Fact]
    public void AssemblyDefaultCanBeOverridden()
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            new TestSourceBuilderOptions(MapOnlyPrimitives: false),
            "class A { public A Child { get; set; } }",
            "class B { public A Child { get; set; } }"
        );
        source = source.Replace("[Mapper(", "[assembly: MapperDefaults(MapOnlyPrimitives = true)]\n[Mapper(", StringComparison.Ordinal);
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::B();
                target.Child = source.Child;
                return target;
                """
            );
    }
}
