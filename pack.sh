#!/usr/bin/env bash
# Builds the mod and assembles the Thunderstore upload zip into dist/.
set -euo pipefail
cd "$(dirname "$0")"

dotnet build TruckOrganizer/TruckOrganizer.csproj -c Release

version=$(python3 -c "import json; print(json.load(open('thunderstore/manifest.json'))['version_number'])")
stage=dist/stage

rm -rf "$stage"
mkdir -p "$stage"

cp thunderstore/manifest.json thunderstore/icon.png thunderstore/CHANGELOG.md README.md "$stage/"
cp TruckOrganizer/bin/Release/netstandard2.1/TruckOrganizer.dll "$stage/"

# Optional: bundle the custom models if they have been built (see docs/ASSETS.md).
[ -f assets/truckorganizer ] && cp assets/truckorganizer "$stage/"

zip_path="dist/TruckOrganizer-$version.zip"
rm -f "$zip_path"
(cd "$stage" && zip -r "../$(basename "$zip_path")" . >/dev/null)
rm -rf "$stage"

echo "Created $zip_path"
