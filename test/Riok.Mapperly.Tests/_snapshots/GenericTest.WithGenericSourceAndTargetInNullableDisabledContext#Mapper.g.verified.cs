﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private partial TTarget? Map<TSource, TTarget>(TSource? source)
    {
        return source switch
        {
            global::A x when typeof(TTarget).IsAssignableFrom(typeof(global::B)) => (TTarget?)(object)MapToB(x),
            global::C x when typeof(TTarget).IsAssignableFrom(typeof(global::D)) => (TTarget?)(object)MapToD(x),
            null => default,
            _ => throw new System.ArgumentException($"Cannot map {source.GetType()} to {typeof(TTarget)} as there is no known type mapping", nameof(source)),
        };
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private partial global::B MapToB(global::A source)
    {
        var target = new global::B();
        target.Value = source.Value;
        return target;
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private partial global::D? MapToD(global::C? source)
    {
        if (source == null)
            return default;
        var target = new global::D(source.Value1);
        return target;
    }
}