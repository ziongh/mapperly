﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private partial global::B Map(global::A a, global::Riok.Mapperly.Abstractions.ReferenceHandling.IReferenceHandler refHandler)
    {
        if (refHandler.TryGetReference<global::A, global::B>(a, out var existingTargetReference))
            return existingTargetReference;
        var target = CreateB();
        refHandler.SetReference<global::A, global::B>(a, target);
        target.Parent = Map(a.Parent, refHandler);
        target.Value = MapToD(a.Value, refHandler);
        return target;
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    private global::D MapToD(global::C source, global::Riok.Mapperly.Abstractions.ReferenceHandling.IReferenceHandler refHandler)
    {
        if (refHandler.TryGetReference<global::C, global::D>(source, out var existingTargetReference))
            return existingTargetReference;
        var target = new global::D();
        refHandler.SetReference<global::C, global::D>(source, target);
        target.StringValue = source.StringValue;
        return target;
    }
}