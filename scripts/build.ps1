#!/usr/bin/env pwsh
# Build the DLL and (if godot is on PATH) export the PCK.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

Write-Host "==> Building C# DLL..." -ForegroundColor Cyan
dotnet build "$root/src/sts2-ultrawide.sln" -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

$dll = "$root/src/SlayTheSpire2Ultrawide/bin/Release/sts2-ultrawide.dll"
if (-not (Test-Path $dll)) { throw "DLL not produced at $dll" }
Write-Host "DLL ready: $dll" -ForegroundColor Green

$godot = Get-Command godot -ErrorAction SilentlyContinue
if (-not $godot) {
    Write-Warning "godot not on PATH - skipping PCK export. Install Godot 4 or set GODOT_BIN."
    return
}
if (-not (Test-Path "$root/godot_project/project.godot")) {
    Write-Warning "godot_project/ not yet created - skipping PCK export."
    return
}

Write-Host "==> Exporting PCK..." -ForegroundColor Cyan
$out = "$root/out/sts2-ultrawide.pck"
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
& godot --headless --path "$root/godot_project" --export-pack "ModPCK" $out
if ($LASTEXITCODE -ne 0) { throw "godot export failed" }
Write-Host "PCK ready: $out" -ForegroundColor Green
