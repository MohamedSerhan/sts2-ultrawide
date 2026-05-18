#!/usr/bin/env pwsh
# Extract textures from SlayTheSpire2.pck for use as outpainting sources.
# Requires `gdsdecomp` (gdsdecomp/godotsteam) on PATH.
$ErrorActionPreference = 'Stop'
$gamePck = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
$out = "C:\temp\sts2-extracted"
if (-not (Get-Command gdsdecomp -ErrorAction SilentlyContinue)) {
    throw "Install gdsdecomp from https://github.com/bruvzg/gdsdecomp/releases and add it to PATH."
}
gdsdecomp --recover $gamePck --output-dir $out
Write-Host "Extracted to $out — copy background plates into assets-src/." -ForegroundColor Green
