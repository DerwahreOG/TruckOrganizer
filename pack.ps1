# Builds the mod and assembles the Thunderstore upload zip into dist/.
# Usage: powershell -ExecutionPolicy Bypass -File pack.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

dotnet build "$root/TruckOrganizer/TruckOrganizer.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$manifest = Get-Content "$root/thunderstore/manifest.json" | ConvertFrom-Json
$version = $manifest.version_number
$stage = "$root/dist/stage"

Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Copy-Item "$root/thunderstore/manifest.json" $stage
Copy-Item "$root/thunderstore/icon.png" $stage
Copy-Item "$root/thunderstore/CHANGELOG.md" $stage
Copy-Item "$root/README.md" $stage
Copy-Item "$root/TruckOrganizer/bin/Release/netstandard2.1/TruckOrganizer.dll" $stage

# Optional: bundle the custom models if they have been built (see docs/ASSETS.md).
$bundle = "$root/assets/truckorganizer"
if (Test-Path $bundle) { Copy-Item $bundle $stage }

$zip = "$root/dist/TruckOrganizer-$version.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path "$stage/*" -DestinationPath $zip
Remove-Item -Recurse -Force $stage

Write-Host "Created $zip"
