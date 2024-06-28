﻿// <auto-generated />
#nullable enable
namespace Riok.Mapperly.IntegrationTests.Mapper
{
    public static partial class DeepCloningMapper
    {
        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        public static partial global::Riok.Mapperly.IntegrationTests.Models.IdObject Copy(global::Riok.Mapperly.IntegrationTests.Models.IdObject src)
        {
            var target = new global::Riok.Mapperly.IntegrationTests.Models.IdObject();
            target.IdValue = src.IdValue;
            return target;
        }

        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        public static partial global::Riok.Mapperly.IntegrationTests.Models.TestObject Copy(global::Riok.Mapperly.IntegrationTests.Models.TestObject src)
        {
            var target = new global::Riok.Mapperly.IntegrationTests.Models.TestObject(src.CtorValue, ctorValue2: src.CtorValue2)
            {
                IntInitOnlyValue = src.IntInitOnlyValue,
                RequiredValue = src.RequiredValue,
            };
            if (src.NullableFlattening != null)
            {
                target.NullableFlattening = Copy(src.NullableFlattening);
            }
            else
            {
                target.NullableFlattening = null;
            }
            if (src.NestedNullable != null)
            {
                target.NestedNullable = MapToTestObjectNested(src.NestedNullable);
            }
            else
            {
                target.NestedNullable = null;
            }
            if (src.NestedNullableTargetNotNullable != null)
            {
                target.NestedNullableTargetNotNullable = MapToTestObjectNested(src.NestedNullableTargetNotNullable);
            }
            else
            {
                target.NestedNullableTargetNotNullable = null;
            }
            if (src.NestedMember != null)
            {
                target.NestedMember = MapToTestObjectNestedMember(src.NestedMember);
            }
            else
            {
                target.NestedMember = null;
            }
            if (src.TupleValue != null)
            {
                target.TupleValue = MapToValueTupleOfStringAndString(src.TupleValue.Value);
            }
            else
            {
                target.TupleValue = null;
            }
            if (src.RecursiveObject != null)
            {
                target.RecursiveObject = Copy(src.RecursiveObject);
            }
            else
            {
                target.RecursiveObject = null;
            }
            if (src.SourceTargetSameObjectType != null)
            {
                target.SourceTargetSameObjectType = Copy(src.SourceTargetSameObjectType);
            }
            else
            {
                target.SourceTargetSameObjectType = null;
            }
            if (src.NullableReadOnlyObjectCollection != null)
            {
                target.NullableReadOnlyObjectCollection = MapToTestObjectNestedArray(src.NullableReadOnlyObjectCollection);
            }
            else
            {
                target.NullableReadOnlyObjectCollection = null;
            }
            if (src.SubObject != null)
            {
                target.SubObject = MapToInheritanceSubObject(src.SubObject);
            }
            else
            {
                target.SubObject = null;
            }
            target.IntValue = src.IntValue;
            target.StringValue = src.StringValue;
            target.RenamedStringValue = src.RenamedStringValue;
            target.Flattening = Copy(src.Flattening);
            target.UnflatteningIdValue = src.UnflatteningIdValue;
            target.NullableUnflatteningIdValue = src.NullableUnflatteningIdValue;
            target.StringNullableTargetNotNullable = src.StringNullableTargetNotNullable;
            target.MemoryValue = src.MemoryValue.Span.ToArray();
            target.StackValue = new global::System.Collections.Generic.Stack<string>(src.StackValue);
            target.QueueValue = new global::System.Collections.Generic.Queue<string>(src.QueueValue);
            target.ImmutableArrayValue = global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(src.ImmutableArrayValue);
            target.ImmutableListValue = global::System.Collections.Immutable.ImmutableList.ToImmutableList(src.ImmutableListValue);
            target.ImmutableQueueValue = global::System.Collections.Immutable.ImmutableQueue.CreateRange(src.ImmutableQueueValue);
            target.ImmutableStackValue = global::System.Collections.Immutable.ImmutableStack.CreateRange(src.ImmutableStackValue);
            target.ImmutableSortedSetValue = global::System.Collections.Immutable.ImmutableSortedSet.ToImmutableSortedSet(src.ImmutableSortedSetValue);
            target.ImmutableDictionaryValue = global::System.Collections.Immutable.ImmutableDictionary.ToImmutableDictionary(src.ImmutableDictionaryValue);
            target.ImmutableSortedDictionaryValue = global::System.Collections.Immutable.ImmutableSortedDictionary.ToImmutableSortedDictionary(src.ImmutableSortedDictionaryValue);
            foreach (var item in src.ExistingISet)
            {
                target.ExistingISet.Add(item);
            }
            target.ExistingHashSet.EnsureCapacity(src.ExistingHashSet.Count + target.ExistingHashSet.Count);
            foreach (var item1 in src.ExistingHashSet)
            {
                target.ExistingHashSet.Add(item1);
            }
            foreach (var item2 in src.ExistingSortedSet)
            {
                target.ExistingSortedSet.Add(item2);
            }
            target.ExistingList.EnsureCapacity(src.ExistingList.Count + target.ExistingList.Count);
            foreach (var item3 in src.ExistingList)
            {
                target.ExistingList.Add(item3);
            }
            target.ISet = global::System.Linq.Enumerable.ToHashSet(src.ISet);
            target.IReadOnlySet = global::System.Linq.Enumerable.ToHashSet(src.IReadOnlySet);
            target.HashSet = global::System.Linq.Enumerable.ToHashSet(src.HashSet);
            target.SortedSet = new global::System.Collections.Generic.SortedSet<string>(src.SortedSet);
            target.EnumValue = src.EnumValue;
            target.FlagsEnumValue = src.FlagsEnumValue;
            target.EnumName = src.EnumName;
            target.EnumRawValue = src.EnumRawValue;
            target.EnumStringValue = src.EnumStringValue;
            target.EnumReverseStringValue = src.EnumReverseStringValue;
            target.DateTimeValue = src.DateTimeValue;
            target.DateTimeValueTargetDateOnly = src.DateTimeValueTargetDateOnly;
            target.DateTimeValueTargetTimeOnly = src.DateTimeValueTargetTimeOnly;
            target.SumComponent1 = src.SumComponent1;
            target.SumComponent2 = src.SumComponent2;
            return target;
        }

        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        private static global::Riok.Mapperly.IntegrationTests.Models.TestObjectNested MapToTestObjectNested(global::Riok.Mapperly.IntegrationTests.Models.TestObjectNested source)
        {
            var target = new global::Riok.Mapperly.IntegrationTests.Models.TestObjectNested();
            target.IntValue = source.IntValue;
            return target;
        }

        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        private static global::Riok.Mapperly.IntegrationTests.Models.TestObjectNestedMember MapToTestObjectNestedMember(global::Riok.Mapperly.IntegrationTests.Models.TestObjectNestedMember source)
        {
            var target = new global::Riok.Mapperly.IntegrationTests.Models.TestObjectNestedMember();
            if (source.NestedMemberObject != null)
            {
                target.NestedMemberObject = MapToTestObjectNested(source.NestedMemberObject);
            }
            else
            {
                target.NestedMemberObject = null;
            }
            target.NestedMemberId = source.NestedMemberId;
            return target;
        }

        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        private static (string A, string) MapToValueTupleOfStringAndString((string A, string) source)
        {
            var target = (A: source.A, source.Item2);
            return target;
        }

        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        private static global::Riok.Mapperly.IntegrationTests.Models.TestObjectNested[] MapToTestObjectNestedArray(global::System.Collections.Generic.IReadOnlyCollection<global::Riok.Mapperly.IntegrationTests.Models.TestObjectNested> source)
        {
            var target = new global::Riok.Mapperly.IntegrationTests.Models.TestObjectNested[source.Count];
            var i = 0;
            foreach (var item in source)
            {
                target[i] = MapToTestObjectNested(item);
                i++;
            }
            return target;
        }

        [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
        private static global::Riok.Mapperly.IntegrationTests.Models.InheritanceSubObject MapToInheritanceSubObject(global::Riok.Mapperly.IntegrationTests.Models.InheritanceSubObject source)
        {
            var target = new global::Riok.Mapperly.IntegrationTests.Models.InheritanceSubObject();
            target.SubIntValue = source.SubIntValue;
            target.BaseIntValue = source.BaseIntValue;
            return target;
        }
    }
}