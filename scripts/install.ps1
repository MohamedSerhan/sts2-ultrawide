#!/usr/bin/env pwsh
# Copy the built mod into the live STS2 install for testing.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$gameMods = $env:STS2_MODS_DIR
if (-not $gameMods) {
    $gameMods = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods"
}
if (-not (Test-Path $gameMods)) { throw "STS2 mods dir not found at $gameMods. Set STS2_MODS_DIR env var." }

$dest = Join-Path $gameMods "sts2-ultrawide"
if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
New-Item -ItemType Directory -Force $dest | Out-Null

Copy-Item "$root/mod_manifest.json" $dest
Copy-Item "$root/compat-target.txt" $dest
Copy-Item "$root/src/SlayTheSpire2Ultrawide/bin/Release/sts2-ultrawide.dll" $dest
if (Test-Path "$root/out/sts2-ultrawide.pck") {
    Copy-Item "$root/out/sts2-ultrawide.pck" $dest
}
Write-Host "Installed to: $dest" -ForegroundColor Green
