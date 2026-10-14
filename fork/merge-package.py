"""Merge upstream Roslyn-specific analyzer directories, retaining one package manifest."""
import pathlib
import sys
import zipfile

root, output, *versions = sys.argv[1:]
if not versions:
    raise SystemExit("No Roslyn versions supplied")

packages = []
for version in versions:
    matches = list((pathlib.Path(root) / version).glob("*.nupkg"))
    if len(matches) != 1:
        raise SystemExit(f"Expected exactly one package for Roslyn {version}: {matches}")
    with zipfile.ZipFile(matches[0]) as package:
        packages.append((version, {name: package.read(name) for name in package.namelist()}))

entries = dict(packages[-1][1])
for version, package in packages:
    prefix = f"analyzers/roslyn{version}/"
    if not any(name.startswith(prefix) for name in package):
        raise SystemExit(f"Missing analyzers for Roslyn {version}")
    for name, data in package.items():
        if name.startswith(prefix):
            entries[name] = data
        elif name.startswith(("lib/", "build/")) and entries.get(name) != data:
            raise SystemExit(f"Shared asset differs across Roslyn builds: {name}")

output = pathlib.Path(output)
temporary = output.with_suffix(".nupkg.tmp")
with zipfile.ZipFile(temporary, "w", compression=zipfile.ZIP_DEFLATED) as package:
    for name, data in sorted(entries.items()):
        package.writestr(name, data)
temporary.replace(output)
