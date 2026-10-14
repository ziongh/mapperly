"""Exercise the packed fork in a clean SDK-selected consumer, not a project reference.
Usage: python3 fork/smoke-package.py PACKAGE.nupkg net10.0 10.0.400
"""
import json
import pathlib
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

package = pathlib.Path(sys.argv[1]).resolve()
framework, sdk = sys.argv[2:4]
with zipfile.ZipFile(package) as archive:
    nuspec = next(name for name in archive.namelist() if name.endswith(".nuspec"))
    metadata = ET.fromstring(archive.read(nuspec))
    ns = {"n": metadata.tag.partition("}")[0].lstrip("{")}
    package_id = metadata.find("n:metadata/n:id", ns).text
    version = metadata.find("n:metadata/n:version", ns).text
    assert package_id == "GreatUtilities.MapperWithPrimitiveOnly"
    assert "build/GreatUtilities.MapperWithPrimitiveOnly.targets" in archive.namelist()

with tempfile.TemporaryDirectory(prefix="mapperly-fork-consumer-") as temporary:
    root = pathlib.Path(temporary)
    (root / "global.json").write_text(json.dumps({"sdk": {"version": sdk, "rollForward": "disable"}}))
    config = ET.Element("configuration")
    sources = ET.SubElement(config, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", key="fork", value=str(package.parent))
    ET.SubElement(sources, "add", key="nuget", value="https://api.nuget.org/v3/index.json")
    ET.ElementTree(config).write(root / "NuGet.Config", encoding="utf-8", xml_declaration=True)
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    properties = ET.SubElement(project, "PropertyGroup")
    for key, value in {
        "OutputType": "Exe", "TargetFramework": framework, "Nullable": "enable",
        "ImplicitUsings": "enable", "TreatWarningsAsErrors": "true",
        "MapperlyMapOnlyPrimitives": "true", "RestorePackagesPath": str(root / "packages"),
    }.items():
        ET.SubElement(properties, key).text = value
    ET.SubElement(ET.SubElement(project, "ItemGroup"), "PackageReference", Include=package_id, Version=version)
    ET.ElementTree(project).write(root / "Consumer.csproj", encoding="utf-8", xml_declaration=True)
    (root / "Program.cs").write_text('''
using Riok.Mapperly.Abstractions;
var source = new Row { Id = Key.From(7), OwnerId = Key.From(9), Tags = ["one"] };
var clone = Copies.Clone(source);
if (ReferenceEquals(source, clone) || clone.Id.Value != 7 || clone.OwnerId?.Value != 9)
    throw new Exception("Typed/nullable keys were not preserved");
clone.Tags.Add("two");
if (source.Tags.Count != 1) throw new Exception("Scalar collection was shared");
Console.WriteLine("PASS: packed analyzer, MSBuild option import, typed/nullable keys, ignored navigation and independent list");
public readonly struct Key
{
    private readonly long value;
    private Key(long value) => this.value = value;
    public long Value => value;
    public static Key From(long value) => value > 0 ? new Key(value) : throw new ArgumentOutOfRangeException(nameof(value));
}
public class Row
{
    public Key Id { get; set; }
    public Key? OwnerId { get; set; }
    public List<string> Tags { get; set; } = [];
    public Row? Parent { get => throw new Exception("Navigation read"); set => throw new Exception("Navigation write"); }
}
[Mapper(UseDeepCloning = true)]
public static partial class Copies { public static partial Row Clone(Row source); }
''')
    subprocess.run(["dotnet", "run", "--project", "Consumer.csproj", "--nologo", "--disable-build-servers"], cwd=root, check=True)
