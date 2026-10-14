#!/usr/bin/env bash
# Publish only a clean, pushed source revision with valid package provenance.
set -Eeuo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$root"
: "${RELEASE_VERSION:?Set RELEASE_VERSION}"
: "${NUGET_API_KEY:?Set NUGET_API_KEY without committing it}"
[[ "$RELEASE_VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || { echo 'Invalid RELEASE_VERSION' >&2; exit 1; }
[[ -z "$(git status --porcelain)" ]] || { echo 'Refusing to publish a dirty checkout' >&2; exit 1; }
[[ "$(git branch --show-current)" == primitive-only-maintained ]] || { echo 'Switch to primitive-only-maintained' >&2; exit 1; }
commit=$(git rev-parse HEAD)
remote_commit=$(git ls-remote origin refs/heads/primitive-only-maintained | awk '{print $1}')
[[ "$commit" == "$remote_commit" ]] || { echo 'Push the reviewed source revision before publishing' >&2; exit 1; }
package="$root/artifacts/fork/$RELEASE_VERSION/GreatUtilities.MapperWithPrimitiveOnly.$RELEASE_VERSION.nupkg"
python3 - "$package" "$commit" <<'PY'
import sys
import xml.etree.ElementTree as ET
import zipfile
with zipfile.ZipFile(sys.argv[1]) as archive:
    name = next(name for name in archive.namelist() if name.endswith('.nuspec'))
    spec = ET.fromstring(archive.read(name))
    ns = {'n': spec.tag.partition('}')[0].lstrip('{')}
    metadata = spec.find('n:metadata', ns)
    assert metadata.find('n:id', ns).text == 'GreatUtilities.MapperWithPrimitiveOnly'
    assert metadata.find('n:repository', ns).get('commit') == sys.argv[2], 'Rebuild the package from the committed source revision'
PY
dotnet meziantou.validate-nuget-package "$package"
dotnet nuget push "$package" --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY"
