using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Riok.Mapperly.Tests;

public class MapperGenerationResultAssertions
{
    private readonly MapperGenerationResult _mapper;
    private readonly HashSet<Diagnostic> _notAssertedDiagnostics;

    public MapperGenerationResultAssertions(MapperGenerationResult mapper)
    {
        _mapper = mapper;
        _notAssertedDiagnostics = new HashSet<Diagnostic>(_mapper.Diagnostics);
    }

    public MapperGenerationResultAssertions HaveDiagnostics()
    {
        _mapper.Diagnostics.Should().NotBeEmpty();
        return this;
    }

    public MapperGenerationResultAssertions HaveAssertedAllDiagnostics()
    {
        _notAssertedDiagnostics.Should().BeEmpty();
        return this;
    }

    public MapperGenerationResultAssertions OnlyHaveDiagnosticSeverities(IReadOnlySet<DiagnosticSeverity> allowedDiagnosticSeverities)
    {
        _mapper.Diagnostics.FirstOrDefault(d => !allowedDiagnosticSeverities.Contains(d.Severity)).Should().BeNull();
        return this;
    }

    public MapperGenerationResultAssertions HaveDiagnostics(DiagnosticDescriptor descriptor, params string[] messages)
    {
        var i = 0;
        foreach (var diagnostic in GetDiagnostics(descriptor))
        {
            diagnostic.GetMessage().Should().Be(messages[i]);
            _notAssertedDiagnostics.Remove(diagnostic);
            i++;
        }

        return this;
    }

    public MapperGenerationResultAssertions HaveDiagnostic(DiagnosticDescriptor descriptor)
    {
        foreach (var diagnostic in GetDiagnostics(descriptor))
        {
            _notAssertedDiagnostics.Remove(diagnostic);
        }

        return this;
    }

    public MapperGenerationResultAssertions HaveDiagnostic(DiagnosticDescriptor descriptor, string message)
    {
        var diagnostics = GetDiagnostics(descriptor);
        var matchedDiagnostic = diagnostics.FirstOrDefault(x => x.GetMessage().Equals(message));
        if (matchedDiagnostic != null)
        {
            _notAssertedDiagnostics.Remove(matchedDiagnostic);
            return this;
        }

        var matchingIdDiagnostic =
            _notAssertedDiagnostics.FirstOrDefault(x => x.Descriptor.Equals(descriptor))
            ?? _mapper.Diagnostics.First(x => x.Descriptor.Equals(descriptor));
        matchingIdDiagnostic.GetMessage().Should().Be(message, $"message of {descriptor.Id} should match");
        return this;
    }

    public MapperGenerationResultAssertions HaveSingleMethodBody([StringSyntax(StringSyntax.CSharp)] string mapperMethodBody)
    {
        _mapper.Methods.Single().Value.Body.Should().Be(mapperMethodBody.ReplaceLineEndings());
        return this;
    }

    public MapperGenerationResultAssertions HaveMethodCount(int count)
    {
        _mapper.Methods.Should().HaveCount(count);
        return this;
    }

    public MapperGenerationResultAssertions AllMethodsHaveBody([StringSyntax(LanguageNames.CSharp)] string mapperMethodBody)
    {
        mapperMethodBody = mapperMethodBody.ReplaceLineEndings().Trim();
        foreach (var method in _mapper.Methods.Values)
        {
            method.Body.Should().Be(mapperMethodBody);
        }

        return this;
    }

    public MapperGenerationResultAssertions HaveMethods(params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            _mapper.Methods.Keys.Should().Contain(methodName);
        }

        return this;
    }

    public MapperGenerationResultAssertions HaveOnlyMethods(params string[] methodNames)
    {
        HaveMethods(methodNames);
        HaveMethodCount(methodNames.Length);
        return this;
    }

    public MapperGenerationResultAssertions HaveMethodBody(string methodName, [StringSyntax(StringSyntax.CSharp)] string mapperMethodBody)
    {
        _mapper.Methods[methodName].Body.Should().Be(mapperMethodBody.ReplaceLineEndings().Trim(), $"Method: {methodName}");
        return this;
    }

    public MapperGenerationResultAssertions HaveMapMethodBody([StringSyntax(StringSyntax.CSharp)] string mapperMethodBody) =>
        HaveMethodBody(TestSourceBuilder.DefaultMapMethodName, mapperMethodBody);

    private IReadOnlyCollection<Diagnostic> GetDiagnostics(DiagnosticDescriptor descriptor)
    {
        if (_mapper.DiagnosticsByDescriptorId.TryGetValue(descriptor.Id, out var diagnostics))
            return diagnostics;

        var foundIds = string.Join(", ", _mapper.Diagnostics.Select(x => x.Descriptor.Id));
        throw new InvalidOperationException($"No diagnostic with id {descriptor.Id} found, found diagnostic ids: {foundIds}");
    }
}
