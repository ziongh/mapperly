"""Regression tests for the fork's multi-Roslyn NuGet merger (standard library only)."""
import pathlib
import subprocess
import sys
import tempfile
import unittest
import zipfile


class MergePackageTests(unittest.TestCase):
    def run_merge(self, *, differing_asset=False, missing_analyzer=False, duplicate_package=False):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            for version in ("4.8", "5.0"):
                folder = root / version
                folder.mkdir()
                entries = {
                    "lib/netstandard2.0/Abstractions.dll": b"same",
                    "build/Fork.targets": b"same",
                    "Fork.nuspec": b"metadata",
                    "_rels/.rels": version.encode(),
                }
                if not (missing_analyzer and version == "4.8"):
                    entries[f"analyzers/roslyn{version}/dotnet/cs/Generator.dll"] = version.encode()
                if differing_asset and version == "4.8":
                    entries["lib/netstandard2.0/Abstractions.dll"] = b"different"
                for name in (["one", "two"] if duplicate_package and version == "4.8" else ["one"]):
                    with zipfile.ZipFile(folder / f"{name}.nupkg", "w") as package:
                        for entry, data in entries.items():
                            package.writestr(entry, data)
            output = root / "combined.nupkg"
            result = subprocess.run(
                [sys.executable, str(pathlib.Path(__file__).with_name("merge-package.py")), str(root), str(output), "4.8", "5.0"],
                capture_output=True, text=True,
            )
            contents = None
            if output.exists():
                with zipfile.ZipFile(output) as package:
                    contents = {name: package.read(name) for name in package.namelist()}
            return result, contents

    def test_combines_analyzers_and_retains_one_manifest(self):
        result, contents = self.run_merge()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(contents["analyzers/roslyn4.8/dotnet/cs/Generator.dll"], b"4.8")
        self.assertEqual(contents["analyzers/roslyn5.0/dotnet/cs/Generator.dll"], b"5.0")
        self.assertEqual(contents["_rels/.rels"], b"5.0")

    def test_rejects_incompatible_shared_assets(self):
        result, contents = self.run_merge(differing_asset=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Shared asset differs", result.stderr)
        self.assertIsNone(contents)

    def test_rejects_missing_analyzer(self):
        result, contents = self.run_merge(missing_analyzer=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Missing analyzers", result.stderr)
        self.assertIsNone(contents)

    def test_rejects_ambiguous_package_selection(self):
        result, contents = self.run_merge(duplicate_package=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Expected exactly one package", result.stderr)
        self.assertIsNone(contents)


if __name__ == "__main__":
    unittest.main()
