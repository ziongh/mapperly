#!/usr/bin/env bash
# Build the fork's NuGet package for every Roslyn version supported by upstream.
set -Eeuo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
: "${RELEASE_VERSION:?Set RELEASE_VERSION to an explicit NuGet prerelease version}"
[[ "$RELEASE_VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || { echo 'Invalid RELEASE_VERSION' >&2; exit 1; }
output="$root/artifacts/fork/$RELEASE_VERSION"
mkdir -p "$output"
work=$(mktemp -d)
trap 'rm -rf -- "$work"' EXIT
mapfile -t versions < <(find "$root/src/Riok.Mapperly" -maxdepth 1 -name 'Riok.Mapperly.Roslyn*.props' -printf '%f\n' | sort -V | awk '{sub(/^Riok.Mapperly.Roslyn/, ""); sub(/\.props$/, ""); print}')
(( ${#versions[@]} > 0 )) || { echo 'No upstream Roslyn targets found' >&2; exit 1; }
for version in "${versions[@]}"; do
  dotnet pack "$root/src/Riok.Mapperly/Riok.Mapperly.csproj" -c Release --nologo \
    -p:ROSLYN_VERSION="$version" -p:Version="$RELEASE_VERSION" \
    -p:CustomAfterMicrosoftCommonTargets="$root/fork/Package.targets" \
    -p:PackageReleaseNotes="Upstream $(git -C "$root" rev-parse upstream/main) - opt-in MapOnlyPrimitives and typed-key support." \
    -o "$work/$version"
done
python3 "$root/fork/merge-package.py" "$work" "$output/GreatUtilities.MapperWithPrimitiveOnly.$RELEASE_VERSION.nupkg" "${versions[@]}"
printf 'Package: %s\n' "$output/GreatUtilities.MapperWithPrimitiveOnly.$RELEASE_VERSION.nupkg"
