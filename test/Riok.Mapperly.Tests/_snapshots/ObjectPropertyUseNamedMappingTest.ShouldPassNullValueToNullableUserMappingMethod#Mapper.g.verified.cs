﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    public partial global::B Map(global::A source)
    {
        var target = new global::B();
        if (source.Value2 != null)
        {
            target.Value2 = source.Value2;
        }
        target.Value = MapString(source.Value);
        return target;
    }
}