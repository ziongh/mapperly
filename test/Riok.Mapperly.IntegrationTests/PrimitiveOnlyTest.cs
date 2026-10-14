using System;
using System.Collections.Generic;
using Riok.Mapperly.Abstractions;
using Shouldly;
using Xunit;

namespace Riok.Mapperly.IntegrationTests;

public class PrimitiveOnlyTest
{
    [Fact]
    public void CloneSkipsThrowingNavigationGettersAndCopiesScalarCollections()
    {
        var source = new PrimitiveOnlyEntity
        {
            Id = 42,
            Key = PrimitiveOnlyKey.From(17),
            OwnerKey = PrimitiveOnlyKey.From(23),
            Name = "Original",
            Values = new List<string> { "one" },
            Bytes = new byte[] { 1, 2 },
        };

        var clone = PrimitiveOnlyMapper.Clone(source);
        clone.ShouldNotBeSameAs(source);
        clone.Name.ShouldBe("Original");
        clone.Id.ShouldBe(0);
        clone.Key.Value.ShouldBe(17);
        clone.OwnerKey!.Value.Value.ShouldBe(23);
        clone.Values.ShouldNotBeSameAs(source.Values);
        clone.Bytes.ShouldNotBeSameAs(source.Bytes);
        clone.Values.Add("two");
        clone.Bytes[0] = 9;
        source.Values.Count.ShouldBe(1);
        source.Bytes[0].ShouldBe((byte)1);
    }
}

public class PrimitiveOnlyEntity
{
    public int Id { get; set; }
    public PrimitiveOnlyKey Key { get; set; }
    public PrimitiveOnlyKey? OwnerKey { get; set; }
    public string Name { get; set; } = "";
    public List<string> Values { get; set; } = new List<string>();
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public PrimitiveOnlyEntity Parent
    {
        get => throw new InvalidOperationException("Navigation getter must not be evaluated");
        set => throw new InvalidOperationException("Navigation setter must not be evaluated");
    }
    public List<PrimitiveOnlyEntity> Children
    {
        get => throw new InvalidOperationException("Collection navigation must not be evaluated");
        set => throw new InvalidOperationException("Collection navigation must not be evaluated");
    }
}

public readonly struct PrimitiveOnlyKey
{
    private readonly long _value;

    private PrimitiveOnlyKey(long value) => _value = value;

    public long Value => _value;

    public static PrimitiveOnlyKey From(long value) =>
        value > 0 ? new PrimitiveOnlyKey(value) : throw new ArgumentOutOfRangeException(nameof(value));
}

[Mapper(MapOnlyPrimitives = true, UseDeepCloning = true)]
public static partial class PrimitiveOnlyMapper
{
    [MapperIgnoreSource(nameof(PrimitiveOnlyEntity.Id))]
    [MapperIgnoreTarget(nameof(PrimitiveOnlyEntity.Id))]
    public static partial PrimitiveOnlyEntity Clone(PrimitiveOnlyEntity source);
}
