﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    public partial global::B? Map(global::A? src)
    {
        return src == null ? default : src switch
        {
            global::AImpl1 x => MapToBImpl1(x),
            global::AImpl2 x => MapToBImpl2(x),
            _ => throw new System.ArgumentException($"Cannot map {src.GetType()} to B as there is no known derived type mapping", nameof(src)),
        };
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private global::BImpl1 MapToBImpl1(global::AImpl1 source)
    {
        var target = new global::BImpl1();
        target.BaseValue = source.BaseValue;
        target.Value1 = source.Value1;
        return target;
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private global::BImpl2 MapToBImpl2(global::AImpl2 source)
    {
        var target = new global::BImpl2();
        target.BaseValue = source.BaseValue;
        target.Value2 = source.Value2;
        return target;
    }
}