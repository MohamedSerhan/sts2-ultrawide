#!/usr/bin/env pwsh
# Assemble a Nexus-ready zip with the install-shaped folder inside.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$manifest = Get-Content "$root/mod_manifest.json" | ConvertFrom-Json
$version = $manifest.version

$staging = "$root/out/staging/sts2-ultrawide"
$zipPath = "$root/out/sts2-ultrawide-$version.zip"

if (Test-Path "$root/out/staging") { Remove-Item -Recurse -Force "$root/out/staging" }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
New-Item -ItemType Directory -Force $staging | Out-Null

Copy-Item "$root/mod_manifest.json" $staging
Copy-Item "$root/compat-target.txt" $staging
Copy-Item "$root/src/SlayTheSpire2Ultrawide/bin/Release/sts2-ultrawide.dll" $staging
if (Test-Path "$root/out/sts2-ultrawide.pck") {
    Copy-Item "$root/out/sts2-ultrawide.pck" $staging
} else {
    Write-Warning "PCK missing - packaging DLL-only. HUD anchor rules and widened plates will be absent."
}

Compress-Archive -Path "$root/out/staging/sts2-ultrawide" -DestinationPath $zipPath -Force
Write-Host "Packaged: $zipPath" -ForegroundColor Green
