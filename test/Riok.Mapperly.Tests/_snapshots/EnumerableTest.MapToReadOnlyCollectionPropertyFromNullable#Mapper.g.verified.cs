﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private partial global::B Map(global::A source)
    {
        var target = new global::B();
        if (source.Value != null)
        {
            foreach (var item in source.Value)
            {
                target.Value.Add((long)item);
            }
        }
        return target;
    }
}