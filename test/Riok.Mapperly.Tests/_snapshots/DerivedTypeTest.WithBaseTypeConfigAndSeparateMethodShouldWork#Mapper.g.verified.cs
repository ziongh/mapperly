﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    public partial global::B Map(global::A src)
    {
        return src switch
        {
            global::ASubType1 x => Map(x),
            global::ASubType2 x => MapToBSubType2(x),
            _ => throw new System.ArgumentException($"Cannot map {src.GetType()} to B as there is no known derived type mapping", nameof(src)),
        };
    }

    public partial global::BSubType1 Map(global::ASubType1 src)
    {
        var target = new global::BSubType1();
        target.Value1 = src.Value1;
        return target;
    }

    private global::BSubType2 MapToBSubType2(global::ASubType2 source)
    {
        var target = new global::BSubType2();
        target.Value2 = source.Value2;
        target.BaseValueB = source.BaseValueA;
        return target;
    }
}