﻿namespace Riok.Mapperly.Tests.Mapping;

public class ExtensionMethodTest
{
    [Fact]
    public Task ExtensionMapMethodShouldWork()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "static partial B MapToB(this A source);",
            "class A { public int Value { get; set; } }",
            "class B { public int Value { get; set; } }"
        );

        return TestHelper.VerifyGenerator(source);
    }

    [Fact]
    public Task ExtensionUpdateMethodShouldWork()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "static partial void MapToB(this A source, B target);",
            "class A { public int Value { get; set; } }",
            "class B { public int Value { get; set; } }"
        );

        return TestHelper.VerifyGenerator(source);
    }
}
