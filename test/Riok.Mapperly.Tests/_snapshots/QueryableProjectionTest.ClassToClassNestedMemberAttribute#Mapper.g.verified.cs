﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    partial global::System.Linq.IQueryable<global::B> Map(global::System.Linq.IQueryable<global::A> source)
    {
#nullable disable
        return System.Linq.Queryable.Select(source, x => new global::B()
        {
            Id = x.Value.Id,
        });
#nullable enable
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private partial global::B MapToB(global::A source)
    {
        var target = new global::B();
        target.Id = source.Value.Id;
        return target;
    }
}