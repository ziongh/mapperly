using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Configuration;
using Riok.Mapperly.Descriptors.MappingBodyBuilders.BuilderContext;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols;

namespace Riok.Mapperly.Descriptors.MappingBodyBuilders;

/// <summary>
/// Body builder for new instance object member mappings (mappings for which the target object gets created via <code>new()</code>).
/// </summary>
public static class NewInstanceObjectMemberMappingBodyBuilder
{
    public static void BuildMappingBody(MappingBuilderContext ctx, NewInstanceObjectMemberMapping mapping)
    {
        var mappingCtx = new NewInstanceBuilderContext<NewInstanceObjectMemberMapping>(ctx, mapping);
        BuildConstructorMapping(mappingCtx);
        BuildInitMemberMappings(mappingCtx, true);
        mappingCtx.AddDiagnostics();
    }

    public static void BuildMappingBody(MappingBuilderContext ctx, NewInstanceObjectMemberMethodMapping mapping)
    {
        var mappingCtx = new NewInstanceContainerBuilderContext<NewInstanceObjectMemberMethodMapping>(ctx, mapping);
        BuildConstructorMapping(mappingCtx);
        BuildInitMemberMappings(mappingCtx);
        ObjectMemberMappingBodyBuilder.BuildMappingBody(mappingCtx);
    }

    private static void BuildInitMemberMappings(INewInstanceBuilderContext<IMapping> ctx, bool includeAllMembers = false)
    {
        var ignoreCase = ctx.BuilderContext.Configuration.Mapper.PropertyNameMappingStrategy == PropertyNameMappingStrategy.CaseInsensitive;

        var initOnlyTargetMembers = includeAllMembers
            ? ctx.TargetMembers.Values.ToArray()
            : ctx.TargetMembers.Values.Where(x => x.CanOnlySetViaInitializer()).ToArray();
        foreach (var targetMember in initOnlyTargetMembers)
        {
            ctx.TargetMembers.Remove(targetMember.Name);

            if (ctx.MemberConfigsByRootTargetName.Remove(targetMember.Name, out var memberConfigs))
            {
                BuildInitMemberMapping(ctx, targetMember, memberConfigs);
                continue;
            }

            if (
                !ctx.BuilderContext.SymbolAccessor.TryFindMemberPath(
                    ctx.Mapping.SourceType,
                    MemberPathCandidateBuilder.BuildMemberPathCandidates(targetMember.Name),
                    ctx.IgnoredSourceMemberNames,
                    ignoreCase,
                    out var sourceMemberPath
                )
            )
            {
                if (targetMember.IsRequired)
                {
                    ctx.BuilderContext.ReportDiagnostic(
                        DiagnosticDescriptors.RequiredMemberNotMapped,
                        targetMember.Name,
                        ctx.Mapping.TargetType,
                        ctx.Mapping.SourceType
                    );
                }
                else if (ctx.BuilderContext.Configuration.Members.RequiredMappingStrategy.HasFlag(RequiredMappingStrategy.Target))
                {
                    ctx.BuilderContext.ReportDiagnostic(
                        DiagnosticDescriptors.SourceMemberNotFound,
                        targetMember.Name,
                        ctx.Mapping.TargetType,
                        ctx.Mapping.SourceType
                    );
                }

                continue;
            }

            BuildInitMemberMapping(ctx, targetMember, sourceMemberPath);
        }
    }

    private static void BuildInitMemberMapping(
        INewInstanceBuilderContext<IMapping> ctx,
        IMappableMember targetMember,
        IReadOnlyCollection<MemberMappingConfiguration> memberConfigs
    )
    {
        // add configured mapping
        // target paths are not supported (yet), only target properties
        if (memberConfigs.Count > 1)
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.MultipleConfigurationsForInitOnlyMember,
                targetMember.Type,
                targetMember.Name
            );
        }

        var memberConfig = memberConfigs.First();
        if (memberConfig.Target.Path.Count > 1)
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.InitOnlyMemberDoesNotSupportPaths,
                targetMember.Type,
                memberConfig.Target.FullName
            );
            return;
        }

        if (
            !ctx.BuilderContext.SymbolAccessor.TryFindMemberPath(ctx.Mapping.SourceType, memberConfig.Source.Path, out var sourceMemberPath)
        )
        {
            if (ctx.BuilderContext.Configuration.Members.RequiredMappingStrategy.HasFlag(RequiredMappingStrategy.Target))
            {
                ctx.BuilderContext.ReportDiagnostic(
                    DiagnosticDescriptors.SourceMemberNotFound,
                    targetMember.Name,
                    ctx.Mapping.TargetType,
                    ctx.Mapping.SourceType
                );
            }

            return;
        }

        BuildInitMemberMapping(ctx, targetMember, sourceMemberPath, memberConfig);
    }

    private static void BuildInitMemberMapping(
        INewInstanceBuilderContext<IMapping> ctx,
        IMappableMember targetMember,
        MemberPath sourcePath,
        MemberMappingConfiguration? memberConfig = null
    )
    {
        var targetPath = new MemberPath(new[] { targetMember });
        if (!ObjectMemberMappingBodyBuilder.ValidateMappingSpecification(ctx, sourcePath, targetPath, true))
            return;

        var mappingKey = new TypeMappingKey(sourcePath.MemberType, targetMember.Type, memberConfig?.ToTypeMappingConfiguration());
        var delegateMapping = ctx.BuilderContext.FindOrBuildLooseNullableMapping(mappingKey, diagnosticLocation: memberConfig?.Location);

        if (delegateMapping == null)
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.CouldNotMapMember,
                ctx.Mapping.SourceType,
                sourcePath.FullName,
                sourcePath.Member.Type,
                ctx.Mapping.TargetType,
                targetPath.FullName,
                targetPath.Member.Type
            );
            return;
        }

        if (delegateMapping.Equals(ctx.Mapping))
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.ReferenceLoopInInitOnlyMapping,
                ctx.Mapping.SourceType,
                sourcePath.FullName,
                ctx.Mapping.TargetType,
                targetPath.FullName
            );
            return;
        }

        var setterTargetPath = SetterMemberPath.Build(ctx.BuilderContext, targetPath);
        var memberMapping = ctx.BuildNullMemberMapping(sourcePath, delegateMapping, targetMember.Type);
        var memberAssignmentMapping = new MemberAssignmentMapping(setterTargetPath, memberMapping);
        ctx.AddInitMemberMapping(memberAssignmentMapping);
    }

    private static void BuildConstructorMapping(INewInstanceBuilderContext<IMapping> ctx)
    {
        if (ctx.Mapping.TargetType is not INamedTypeSymbol namedTargetType)
        {
            ctx.BuilderContext.ReportDiagnostic(DiagnosticDescriptors.NoConstructorFound, ctx.BuilderContext.Target);
            return;
        }

        // attributed ctor is prio 1
        // if preferParameterlessConstructors is true (default) :parameterless ctor is prio 2 then by descending parameter count
        // the reverse if preferParameterlessConstructors is false , descending parameter count is prio2 then parameterless ctor
        // ctors annotated with [Obsolete] are considered last unless they have a MapperConstructor attribute set
        var ctorCandidates = namedTargetType
            .InstanceConstructors.Where(ctor => ctx.BuilderContext.SymbolAccessor.IsDirectlyAccessible(ctor))
            .OrderByDescending(x => ctx.BuilderContext.SymbolAccessor.HasAttribute<MapperConstructorAttribute>(x))
            .ThenBy(x => ctx.BuilderContext.SymbolAccessor.HasAttribute<ObsoleteAttribute>(x));

        if (ctx.BuilderContext.Configuration.Mapper.PreferParameterlessConstructors)
        {
            ctorCandidates = ctorCandidates.ThenByDescending(x => x.Parameters.Length == 0).ThenByDescending(x => x.Parameters.Length);
        }
        else
        {
            ctorCandidates = ctorCandidates.ThenByDescending(x => x.Parameters.Length).ThenByDescending(x => x.Parameters.Length == 0);
        }

        foreach (var ctorCandidate in ctorCandidates)
        {
            if (!TryBuildConstructorMapping(ctx, ctorCandidate, out var mappedTargetMemberNames, out var constructorParameterMappings))
            {
                if (ctx.BuilderContext.SymbolAccessor.HasAttribute<MapperConstructorAttribute>(ctorCandidate))
                {
                    ctx.BuilderContext.ReportDiagnostic(
                        DiagnosticDescriptors.CannotMapToConfiguredConstructor,
                        ctx.Mapping.SourceType,
                        ctorCandidate
                    );
                }

                continue;
            }

            ctx.TargetMembers.RemoveRange(mappedTargetMemberNames);
            foreach (var constructorParameterMapping in constructorParameterMappings)
            {
                ctx.AddConstructorParameterMapping(constructorParameterMapping);
            }

            return;
        }

        ctx.BuilderContext.ReportDiagnostic(DiagnosticDescriptors.NoConstructorFound, ctx.BuilderContext.Target);
    }

    private static bool TryBuildConstructorMapping(
        INewInstanceBuilderContext<IMapping> ctx,
        IMethodSymbol ctor,
        [NotNullWhen(true)] out ISet<string>? mappedTargetMemberNames,
        [NotNullWhen(true)] out ISet<ConstructorParameterMapping>? constructorParameterMappings
    )
    {
        constructorParameterMappings = new HashSet<ConstructorParameterMapping>();
        mappedTargetMemberNames = new HashSet<string>();
        var skippedOptionalParam = false;
        foreach (var parameter in ctor.Parameters)
        {
            if (!TryFindConstructorParameterSourcePath(ctx, parameter, out var sourcePath, out var memberConfig))
            {
                // expressions do not allow skipping of optional parameters
                if (!parameter.IsOptional || ctx.BuilderContext.IsExpression)
                    return false;

                skippedOptionalParam = true;
                continue;
            }

            // nullability is handled inside the member mapping
            var parameterType = ctx.BuilderContext.SymbolAccessor.UpgradeNullable(parameter.Type);
            var typeMapping = new TypeMappingKey(sourcePath.MemberType, parameterType, memberConfig?.ToTypeMappingConfiguration());
            var delegateMapping = ctx.BuilderContext.FindOrBuildLooseNullableMapping(
                typeMapping,
                diagnosticLocation: memberConfig?.Location
            );

            if (delegateMapping == null)
            {
                if (!parameter.IsOptional)
                    return false;

                skippedOptionalParam = true;
                continue;
            }

            if (delegateMapping.Equals(ctx.Mapping))
            {
                ctx.BuilderContext.ReportDiagnostic(
                    DiagnosticDescriptors.ReferenceLoopInCtorMapping,
                    ctx.Mapping.SourceType,
                    sourcePath.FullName,
                    ctx.Mapping.TargetType,
                    parameter.Name
                );
                return false;
            }

            var memberMapping = ctx.BuildNullMemberMapping(sourcePath, delegateMapping, parameterType);
            var ctorMapping = new ConstructorParameterMapping(parameter, memberMapping, skippedOptionalParam);
            constructorParameterMappings.Add(ctorMapping);
            mappedTargetMemberNames.Add(parameter.Name);
        }

        return true;
    }

    private static bool TryFindConstructorParameterSourcePath(
        INewInstanceBuilderContext<IMapping> ctx,
        IParameterSymbol parameter,
        [NotNullWhen(true)] out MemberPath? sourcePath,
        out MemberMappingConfiguration? memberConfig
    )
    {
        sourcePath = null;
        memberConfig = null;

        if (
            !ctx.RootTargetNameCasingMapping.TryGetValue(parameter.Name, out var parameterName)
            || !ctx.MemberConfigsByRootTargetName.TryGetValue(parameterName, out var memberConfigs)
        )
        {
            return ctx.BuilderContext.SymbolAccessor.TryFindMemberPath(
                ctx.Mapping.SourceType,
                MemberPathCandidateBuilder.BuildMemberPathCandidates(parameter.Name),
                ctx.IgnoredSourceMemberNames,
                true,
                out sourcePath
            );
        }

        if (memberConfigs.Count > 1)
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.MultipleConfigurationsForConstructorParameter,
                parameter.Type,
                parameter.Name
            );
        }

        memberConfig = memberConfigs.First();
        if (memberConfig.Target.Path.Count > 1)
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.ConstructorParameterDoesNotSupportPaths,
                parameter.Type,
                memberConfig.Target.FullName
            );
            return false;
        }

        if (!ctx.BuilderContext.SymbolAccessor.TryFindMemberPath(ctx.Mapping.SourceType, memberConfig.Source.Path, out sourcePath))
        {
            ctx.BuilderContext.ReportDiagnostic(
                DiagnosticDescriptors.SourceMemberNotFound,
                memberConfig.Source.FullName,
                ctx.Mapping.TargetType,
                ctx.Mapping.SourceType
            );
            return false;
        }

        return true;
    }
}
