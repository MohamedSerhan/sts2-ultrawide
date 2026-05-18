# STS2 Ultrawide Mod Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a working v0.1.0 of `sts2-ultrawide` — an STS2 mod that adds ultrawide resolutions (up to 5120x1440 / 32:9), reflows the HUD to true viewport edges, and ships at least one widened background plate to prove the swap mechanism — plus a GitHub repo and a release pipeline that auto-uploads to an existing Nexus mod page.

**Architecture:** Hybrid .NET DLL + Godot PCK loaded by STS2's built-in mod loader. DLL drives resolution unlock, scene-load hooks, HUD re-anchoring, viewport/camera adjustment, and reads `config.toml`. PCK ships widened background plates plus an `anchor_rules.json` data file. GitHub Action releases on tag and uses `Nexus-Mods/upload-action` to push the file to Nexus.

**Tech Stack:** C# (.NET, target framework discovered in Task 2), Godot 4 (for PCK export only), PowerShell (local build scripts on Windows), GitHub Actions (CI/release), Nexus Mods upload API.

**Key external assumptions:**
- STS2 install at `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2` (`release_info.json` shows v0.103.2).
- `gh` CLI authenticated as `MohamedSerhan` with `repo` + `workflow` scopes.
- Godot 4 editor available locally as `godot` on PATH, or installed to a known path (Task 3 verifies).
- .NET 8 SDK installed locally (Task 2 verifies).

**Scope note:** Per the spec's "Open questions for review", the plan defaults are: mod id = `sts2-ultrawide`, public name = "Ultrawide Support", license = MIT, framework = `net8.0` (verified against an installed mod's DLL in Task 2). If any of those are wrong, fix them in the manifest/README/csproj at the end before tagging v0.1.0.

---

## File structure

```
sts2-ultrawide/
├── README.md                          ← end-user install + dev quickstart
├── LICENSE                            ← MIT
├── .gitignore                         ← (exists)
├── .editorconfig                      ← C# formatting
├── mod_manifest.json                  ← shipped with the mod, source of truth
├── compat-target.txt                  ← shipped with the mod
├── src/
│   ├── sts2-ultrawide.sln
│   ├── SlayTheSpire2Ultrawide/
│   │   ├── SlayTheSpire2Ultrawide.csproj
│   │   ├── Mod.cs                     ← entry point, scene-change hook
│   │   ├── ResolutionUnlocker.cs
│   │   ├── ViewportAdjuster.cs
│   │   ├── HudReanchorer.cs
│   │   ├── BackgroundSwapper.cs
│   │   ├── DebugOverlay.cs
│   │   ├── Config.cs
│   │   ├── AspectMath.cs
│   │   └── AnchorRules.cs
│   ├── SlayTheSpire2Ultrawide.Tests/
│   │   ├── SlayTheSpire2Ultrawide.Tests.csproj
│   │   ├── ConfigTests.cs
│   │   ├── AspectMathTests.cs
│   │   └── AnchorRulesTests.cs
│   └── lib/                           ← copied game DLLs we link against (gitignored)
├── godot_project/
│   ├── project.godot
│   ├── export_presets.cfg
│   └── assets/
│       ├── anchor_rules.json
│       └── backgrounds/combat/3440.png  (etc.)
├── assets-src/                        ← editable source images (PSD, layers, refs)
│   └── backgrounds/combat/3440.psd
├── scripts/
│   ├── build.ps1
│   ├── package.ps1
│   ├── install.ps1
│   └── extract_game_assets.ps1        ← helper to pull originals from SlayTheSpire2.pck
├── .github/workflows/
│   ├── ci.yml
│   └── release.yml
└── docs/
    └── superpowers/
        ├── specs/2026-05-17-ultrawide-mod-design.md   (exists)
        └── plans/2026-05-17-ultrawide-mod.md          (this file)
```

---

## Phase 1 — Repo & tooling foundation

### Task 1: License, README skeleton, and editorconfig

**Files:**
- Create: `LICENSE`
- Create: `README.md`
- Create: `.editorconfig`

- [ ] **Step 1: Write MIT LICENSE**

Create `LICENSE` with the standard MIT template, year `2026`, copyright holder `MohamedSerhan`. Use the canonical text verbatim from `https://opensource.org/license/mit/`.

- [ ] **Step 2: Write README skeleton**

Create `README.md` with these sections (filled-in real text, not placeholders):

````markdown
# STS2 Ultrawide

Adds true ultrawide resolution support to Slay the Spire 2 — up to 5120x1440 (32:9). Backgrounds extend to fill the screen, the HUD re-anchors to the true viewport edges, and the card hand spreads sensibly across the wider playfield. Targets STS2 v0.103.2 on Godot 4.

## Supported resolutions

2560x1080 · 3440x1440 · 3840x1600 · 5120x1440 · 5120x2160

## Install

1. Download `sts2-ultrawide-<version>.zip` from the [latest release](https://github.com/MohamedSerhan/sts2-ultrawide/releases/latest) (or from Nexus Mods).
2. Extract the inner `sts2-ultrawide/` folder into your STS2 install: `<Steam>\steamapps\common\Slay the Spire 2\mods\`.
3. Launch the game. You should see a "Modded" banner. Open Settings → Video; ultrawide entries will appear in the resolution dropdown.

## Configuration

`mods/sts2-ultrawide/config.toml` is generated on first run. Edit to tweak HUD padding, force a specific aspect, or enable the debug overlay.

## Development

See [docs/superpowers/specs/2026-05-17-ultrawide-mod-design.md](docs/superpowers/specs/2026-05-17-ultrawide-mod-design.md) for the design spec and [docs/superpowers/plans/2026-05-17-ultrawide-mod.md](docs/superpowers/plans/2026-05-17-ultrawide-mod.md) for the implementation plan.

Local build (Windows):

```powershell
./scripts/build.ps1      # compiles DLL and exports PCK
./scripts/install.ps1    # copies the built mod into your STS2 install for testing
```

## License

MIT. See [LICENSE](LICENSE).
````

- [ ] **Step 3: Write .editorconfig**

Create `.editorconfig` with:

```ini
root = true

[*]
end_of_line = lf
insert_final_newline = true
charset = utf-8
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{md,yml,yaml,json}]
indent_size = 2

[*.{cs,csproj,sln}]
indent_size = 4
```

- [ ] **Step 4: Commit**

```bash
git add LICENSE README.md .editorconfig
git commit -m "chore: add license, readme, editorconfig"
```

---

### Task 2: Discover STS2 mod loader API and pin .NET target

**Files:**
- Create: `src/lib/.gitkeep`
- Create: `docs/notes/mod-loader-api.md`

This is a **discovery task** — before writing the DLL we have to learn the exact entry-point convention STS2 expects, the namespaces it exposes to mods, and what target framework an installed mod DLL is built against.

- [ ] **Step 1: Copy a reference mod DLL locally**

```powershell
mkdir -Force src/lib
Copy-Item "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/STS2-RitsuLib/STS2-RitsuLib.dll" src/lib/
Copy-Item "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/BaseLib/BaseLib.dll" src/lib/
Copy-Item "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.exe" src/lib/  # optional, for ref only
```

The DLLs are not committed (already covered by `.gitignore` indirectly via `bin/`/`obj/`; explicitly add `src/lib/*.dll` to `.gitignore` below).

- [ ] **Step 2: Inspect a mod DLL for entry-point pattern and framework**

Use ILSpy or `dotnet-ildasm`. If neither is installed, install ILSpy CLI:

```powershell
dotnet tool install -g ilspycmd
```

Then:

```powershell
ilspycmd src/lib/STS2-RitsuLib.dll --project -o docs/notes/ritsulib-decompiled/
```

Read the generated `.csproj` for `<TargetFramework>` — pin this in our project. Read the entry-point class (often named `Mod`, has a `_Init()` or `Load()` method, or uses an attribute like `[Mod]`).

- [ ] **Step 3: Document findings**

Write `docs/notes/mod-loader-api.md` with:
- The target framework string (e.g. `net8.0`, `net6.0`).
- The exact entry-point convention (interface implemented, attribute used, naming convention, signature).
- The Godot namespaces in scope (`Godot`, `Godot.Bridge`, etc.) and which assemblies need to be referenced from `src/lib/` (e.g. `GodotSharp.dll`).
- Any logger / mod-loader services exposed (e.g. how RitsuLib accesses the mod manager).

This document is the source of truth for Tasks 6–13. If the convention turns out to differ from "class named `Mod` with a `_Init` method", revise the relevant later tasks before starting them.

- [ ] **Step 4: Update .gitignore for lib/**

Append to `.gitignore`:

```
# referenced game/mod DLLs are local-only
src/lib/*.dll
src/lib/*.pdb
```

- [ ] **Step 5: Commit**

```bash
git add .gitignore docs/notes/mod-loader-api.md src/lib/.gitkeep
git commit -m "docs: pin STS2 mod loader API conventions and target framework"
```

---

### Task 3: Create C# solution and main project

**Files:**
- Create: `src/sts2-ultrawide.sln`
- Create: `src/SlayTheSpire2Ultrawide/SlayTheSpire2Ultrawide.csproj`
- Create: `src/SlayTheSpire2Ultrawide/Placeholder.cs`

- [ ] **Step 1: Scaffold solution and class library**

```powershell
cd src
dotnet new sln -n sts2-ultrawide
dotnet new classlib -n SlayTheSpire2Ultrawide -f net8.0   # adjust -f if Task 2 found a different framework
dotnet sln add SlayTheSpire2Ultrawide/SlayTheSpire2Ultrawide.csproj
Remove-Item SlayTheSpire2Ultrawide/Class1.cs
```

- [ ] **Step 2: Configure csproj**

Open `src/SlayTheSpire2Ultrawide/SlayTheSpire2Ultrawide.csproj` and replace with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>    <!-- match Task 2 finding -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>sts2-ultrawide</AssemblyName>
    <RootNamespace>SlayTheSpire2Ultrawide</RootNamespace>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="GodotSharp">
      <HintPath>..\lib\GodotSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <!-- Additional references discovered in Task 2 go here -->
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Tomlyn" Version="0.17.0" />   <!-- TOML parsing for config.toml -->
  </ItemGroup>
</Project>
```

If Task 2 found that the mod loader references its own `GodotSharp.dll` from the game's data folder (`data_sts2_windows_x86_64/GodotSharp.dll`), copy that into `src/lib/` first.

- [ ] **Step 3: Add a placeholder file so the project compiles**

Create `src/SlayTheSpire2Ultrawide/Placeholder.cs`:

```csharp
namespace SlayTheSpire2Ultrawide;

internal static class Placeholder
{
    // Replaced in Task 8 by Mod.cs. Exists so the project compiles in isolation.
}
```

- [ ] **Step 4: Verify build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds with `0 Warning(s), 0 Error(s)` and produces `src/SlayTheSpire2Ultrawide/bin/Release/sts2-ultrawide.dll`.

- [ ] **Step 5: Commit**

```bash
git add src/sts2-ultrawide.sln src/SlayTheSpire2Ultrawide/SlayTheSpire2Ultrawide.csproj src/SlayTheSpire2Ultrawide/Placeholder.cs
git commit -m "build: scaffold C# class library project"
```

---

### Task 4: Create test project

**Files:**
- Create: `src/SlayTheSpire2Ultrawide.Tests/SlayTheSpire2Ultrawide.Tests.csproj`
- Create: `src/SlayTheSpire2Ultrawide.Tests/PlaceholderTests.cs`

- [ ] **Step 1: Scaffold xUnit test project**

```powershell
cd src
dotnet new xunit -n SlayTheSpire2Ultrawide.Tests -f net8.0
dotnet sln add SlayTheSpire2Ultrawide.Tests/SlayTheSpire2Ultrawide.Tests.csproj
dotnet add SlayTheSpire2Ultrawide.Tests reference SlayTheSpire2Ultrawide
Remove-Item SlayTheSpire2Ultrawide.Tests/UnitTest1.cs
```

- [ ] **Step 2: Write a sanity test**

Create `src/SlayTheSpire2Ultrawide.Tests/PlaceholderTests.cs`:

```csharp
namespace SlayTheSpire2Ultrawide.Tests;

public class PlaceholderTests
{
    [Fact]
    public void TwoPlusTwoIsFour()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

- [ ] **Step 3: Run and verify pass**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release
```

Expected: `Passed! - Failed: 0, Passed: 1`.

- [ ] **Step 4: Commit**

```bash
git add src/SlayTheSpire2Ultrawide.Tests/
git commit -m "test: scaffold xunit test project"
```

---

### Task 5: Mod manifest and compat-target

**Files:**
- Create: `mod_manifest.json`
- Create: `compat-target.txt`

- [ ] **Step 1: Write manifest**

Create `mod_manifest.json` exactly:

```json
{
  "id": "sts2-ultrawide",
  "pck_name": "sts2-ultrawide",
  "name": "Ultrawide Support",
  "author": "MohamedSerhan",
  "description": "Adds true ultrawide resolution support (up to 5120x1440 / 32:9) with reflowed HUD and widened backgrounds.",
  "version": "0.1.0",
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": false,
  "dependencies": [],
  "min_game_version": "0.103.2"
}
```

- [ ] **Step 2: Write compat-target.txt**

Create `compat-target.txt` with a single line:

```
0.103.2
```

(No trailing whitespace, single newline at EOF.)

- [ ] **Step 3: Commit**

```bash
git add mod_manifest.json compat-target.txt
git commit -m "feat: add mod manifest pinning STS2 v0.103.2"
```

---

### Task 6: Local build/package/install scripts

**Files:**
- Create: `scripts/build.ps1`
- Create: `scripts/package.ps1`
- Create: `scripts/install.ps1`
- Create: `scripts/extract_game_assets.ps1`

- [ ] **Step 1: Write build.ps1**

Create `scripts/build.ps1`:

```powershell
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
    Write-Warning "godot not on PATH — skipping PCK export. Set GODOT_BIN env var or add godot to PATH."
    return
}

Write-Host "==> Exporting PCK..." -ForegroundColor Cyan
$out = "$root/out/sts2-ultrawide.pck"
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
& godot --headless --path "$root/godot_project" --export-pack "ModPCK" $out
if ($LASTEXITCODE -ne 0) { throw "godot export failed" }
Write-Host "PCK ready: $out" -ForegroundColor Green
```

- [ ] **Step 2: Write package.ps1**

Create `scripts/package.ps1`:

```powershell
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
    Write-Warning "PCK missing — packaging without it. Resolution unlock will still work but plate swaps will fail at runtime."
}

Compress-Archive -Path "$root/out/staging/sts2-ultrawide" -DestinationPath $zipPath -Force
Write-Host "Packaged: $zipPath" -ForegroundColor Green
```

- [ ] **Step 3: Write install.ps1**

Create `scripts/install.ps1`:

```powershell
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
```

- [ ] **Step 4: Write extract_game_assets.ps1**

Create `scripts/extract_game_assets.ps1` (a helper, not part of the normal build — used once to seed source assets):

```powershell
#!/usr/bin/env pwsh
# Extract textures from SlayTheSpire2.pck for use as outpainting sources.
# Requires `gdsdecomp` (godot-ses-tools) on PATH.
$ErrorActionPreference = 'Stop'
$gamePck = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck"
$out = "C:\temp\sts2-extracted"
if (-not (Get-Command gdsdecomp -ErrorAction SilentlyContinue)) {
    throw "Install gdsdecomp from https://github.com/bruvzg/gdsdecomp/releases and add it to PATH."
}
gdsdecomp --recover $gamePck --output-dir $out
Write-Host "Extracted to $out — copy background plates into assets-src/." -ForegroundColor Green
```

- [ ] **Step 5: Verify build.ps1 works on the placeholder code**

```powershell
./scripts/build.ps1
```

Expected: DLL builds; warning about missing godot is fine for now (PCK comes later).

- [ ] **Step 6: Commit**

```bash
git add scripts/
git commit -m "build: add local build/package/install/extract scripts"
```

---

## Phase 2 — DLL core (test-driven where applicable)

> Notes on testing in this phase: pure logic (config parsing, aspect math, JSON rules) gets real xUnit tests. Godot-integration code (scene hooks, node-tree walking, texture swapping) can't run under xUnit because the Godot runtime isn't loaded — it gets manual smoke tests in Phase 4 and a tiny debug overlay for in-game verification.

### Task 7: Config.cs with TOML parsing — TDD

**Files:**
- Create: `src/SlayTheSpire2Ultrawide/Config.cs`
- Create: `src/SlayTheSpire2Ultrawide.Tests/ConfigTests.cs`

- [ ] **Step 1: Write failing tests**

Create `src/SlayTheSpire2Ultrawide.Tests/ConfigTests.cs`:

```csharp
using SlayTheSpire2Ultrawide;

namespace SlayTheSpire2Ultrawide.Tests;

public class ConfigTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var c = Config.Defaults();
        Assert.True(c.Enable);
        Assert.Equal("auto", c.ForceAspect);
        Assert.Equal(24, c.HudEdgePaddingPx);
        Assert.Equal(1400, c.CardHandMaxSpreadPx);
        Assert.False(c.DebugOverlay);
        Assert.False(c.VerboseLog);
    }

    [Fact]
    public void Parse_ReadsAllKnownKeys()
    {
        var toml = """
            enable = false
            force_aspect = "5120x1440"
            hud_edge_padding_px = 48
            card_hand_max_spread_px = 1800
            debug_overlay = true
            verbose_log = true
            """;
        var c = Config.Parse(toml);
        Assert.False(c.Enable);
        Assert.Equal("5120x1440", c.ForceAspect);
        Assert.Equal(48, c.HudEdgePaddingPx);
        Assert.Equal(1800, c.CardHandMaxSpreadPx);
        Assert.True(c.DebugOverlay);
        Assert.True(c.VerboseLog);
    }

    [Fact]
    public void Parse_MissingKeys_FallBackToDefaults()
    {
        var c = Config.Parse("enable = false");
        Assert.False(c.Enable);
        Assert.Equal(24, c.HudEdgePaddingPx);   // default preserved
    }

    [Fact]
    public void ToToml_RoundTrips()
    {
        var c = Config.Defaults();
        c.HudEdgePaddingPx = 32;
        var round = Config.Parse(c.ToToml());
        Assert.Equal(32, round.HudEdgePaddingPx);
    }
}
```

- [ ] **Step 2: Run tests, confirm they fail**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release --filter FullyQualifiedName~ConfigTests
```

Expected: 4 failures with "Config does not exist".

- [ ] **Step 3: Implement Config.cs**

Create `src/SlayTheSpire2Ultrawide/Config.cs`:

```csharp
using Tomlyn;
using Tomlyn.Model;

namespace SlayTheSpire2Ultrawide;

public sealed class Config
{
    public bool Enable { get; set; }
    public string ForceAspect { get; set; } = "auto";
    public int HudEdgePaddingPx { get; set; }
    public int CardHandMaxSpreadPx { get; set; }
    public bool DebugOverlay { get; set; }
    public bool VerboseLog { get; set; }

    public static Config Defaults() => new()
    {
        Enable = true,
        ForceAspect = "auto",
        HudEdgePaddingPx = 24,
        CardHandMaxSpreadPx = 1400,
        DebugOverlay = false,
        VerboseLog = false,
    };

    public static Config Parse(string toml)
    {
        var c = Defaults();
        var doc = Toml.ToModel(toml);
        if (doc.TryGetValue("enable", out var v1) && v1 is bool b1) c.Enable = b1;
        if (doc.TryGetValue("force_aspect", out var v2) && v2 is string s2) c.ForceAspect = s2;
        if (doc.TryGetValue("hud_edge_padding_px", out var v3) && v3 is long l3) c.HudEdgePaddingPx = (int)l3;
        if (doc.TryGetValue("card_hand_max_spread_px", out var v4) && v4 is long l4) c.CardHandMaxSpreadPx = (int)l4;
        if (doc.TryGetValue("debug_overlay", out var v5) && v5 is bool b5) c.DebugOverlay = b5;
        if (doc.TryGetValue("verbose_log", out var v6) && v6 is bool b6) c.VerboseLog = b6;
        return c;
    }

    public string ToToml() => $"""
        enable = {Enable.ToString().ToLowerInvariant()}
        force_aspect = "{ForceAspect}"
        hud_edge_padding_px = {HudEdgePaddingPx}
        card_hand_max_spread_px = {CardHandMaxSpreadPx}
        debug_overlay = {DebugOverlay.ToString().ToLowerInvariant()}
        verbose_log = {VerboseLog.ToString().ToLowerInvariant()}
        """;

    public static Config LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = Defaults();
            File.WriteAllText(path, defaults.ToToml());
            return defaults;
        }
        return Parse(File.ReadAllText(path));
    }
}
```

- [ ] **Step 4: Run tests, confirm they pass**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release --filter FullyQualifiedName~ConfigTests
```

Expected: `Passed: 4, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/Config.cs src/SlayTheSpire2Ultrawide.Tests/ConfigTests.cs
git commit -m "feat(config): TOML-backed Config with defaults and round-trip"
```

---

### Task 8: AspectMath.cs — TDD

**Files:**
- Create: `src/SlayTheSpire2Ultrawide/AspectMath.cs`
- Create: `src/SlayTheSpire2Ultrawide.Tests/AspectMathTests.cs`

- [ ] **Step 1: Write failing tests**

Create `src/SlayTheSpire2Ultrawide.Tests/AspectMathTests.cs`:

```csharp
using SlayTheSpire2Ultrawide;

namespace SlayTheSpire2Ultrawide.Tests;

public class AspectMathTests
{
    [Theory]
    [InlineData(1920, 1080, 1.000)]
    [InlineData(2560, 1080, 1.333)]
    [InlineData(3440, 1440, 1.343)]
    [InlineData(3840, 1600, 1.350)]
    [InlineData(5120, 1440, 2.000)]
    [InlineData(5120, 2160, 1.333)]
    public void CameraXMultiplier_MatchesLadder(int w, int h, double expected)
    {
        var m = AspectMath.CameraXMultiplier(w, h);
        Assert.Equal(expected, m, precision: 3);
    }

    [Fact]
    public void IsUltrawide_TrueWhenAspectExceeds16x9PlusEpsilon()
    {
        Assert.False(AspectMath.IsUltrawide(1920, 1080));
        Assert.False(AspectMath.IsUltrawide(1366, 768));
        Assert.True(AspectMath.IsUltrawide(2560, 1080));
        Assert.True(AspectMath.IsUltrawide(5120, 1440));
    }

    [Fact]
    public void ResolutionLadder_ReturnsAllSupportedResolutions()
    {
        var ladder = AspectMath.ResolutionLadder().ToList();
        Assert.Contains((2560, 1080), ladder);
        Assert.Contains((3440, 1440), ladder);
        Assert.Contains((3840, 1600), ladder);
        Assert.Contains((5120, 1440), ladder);
        Assert.Contains((5120, 2160), ladder);
    }
}
```

- [ ] **Step 2: Run tests, confirm they fail**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release --filter FullyQualifiedName~AspectMathTests
```

Expected: failures with "AspectMath does not exist".

- [ ] **Step 3: Implement AspectMath.cs**

Create `src/SlayTheSpire2Ultrawide/AspectMath.cs`:

```csharp
namespace SlayTheSpire2Ultrawide;

public static class AspectMath
{
    public const double BaseAspect = 16.0 / 9.0;

    public static double CameraXMultiplier(int width, int height)
    {
        var aspect = (double)width / height;
        var m = aspect / BaseAspect;
        return Math.Round(m, 3);
    }

    public static bool IsUltrawide(int width, int height)
    {
        const double epsilon = 0.01;
        return (double)width / height > BaseAspect + epsilon;
    }

    public static IEnumerable<(int Width, int Height)> ResolutionLadder() =>
        new[]
        {
            (2560, 1080),
            (3440, 1440),
            (3840, 1600),
            (5120, 1440),
            (5120, 2160),
        };
}
```

- [ ] **Step 4: Run tests, confirm pass**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release --filter FullyQualifiedName~AspectMathTests
```

Expected: `Passed: 8, Failed: 0` (6 theory cases + 2 facts).

- [ ] **Step 5: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/AspectMath.cs src/SlayTheSpire2Ultrawide.Tests/AspectMathTests.cs
git commit -m "feat(math): aspect ratio math and supported resolution ladder"
```

---

### Task 9: AnchorRules.cs — JSON loader, TDD

**Files:**
- Create: `src/SlayTheSpire2Ultrawide/AnchorRules.cs`
- Create: `src/SlayTheSpire2Ultrawide.Tests/AnchorRulesTests.cs`

`AnchorRules` describes which Godot scene nodes get reanchored and how. JSON shape:

```json
{
  "rules": [
    {
      "node_path": "*/HUD/TopBar",
      "anchor_left": 0.0,
      "anchor_right": 1.0,
      "anchor_top": 0.0,
      "anchor_bottom": 0.1,
      "offset_left_px": 24,
      "offset_right_px": -24
    }
  ]
}
```

`node_path` supports a leading `*/` glob meaning "match anywhere in the scene tree".

- [ ] **Step 1: Write failing tests**

Create `src/SlayTheSpire2Ultrawide.Tests/AnchorRulesTests.cs`:

```csharp
using SlayTheSpire2Ultrawide;

namespace SlayTheSpire2Ultrawide.Tests;

public class AnchorRulesTests
{
    private const string SampleJson = """
        {
          "rules": [
            {
              "node_path": "*/HUD/TopBar",
              "anchor_left": 0.0,
              "anchor_right": 1.0,
              "anchor_top": 0.0,
              "anchor_bottom": 0.08,
              "offset_left_px": 24,
              "offset_right_px": -24
            },
            {
              "node_path": "*/HUD/EndTurnButton",
              "anchor_left": 1.0,
              "anchor_right": 1.0,
              "anchor_top": 0.85,
              "anchor_bottom": 1.0,
              "offset_left_px": -200,
              "offset_right_px": -24
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ReadsAllRules()
    {
        var rules = AnchorRules.Parse(SampleJson);
        Assert.Equal(2, rules.Count);
        Assert.Equal("*/HUD/TopBar", rules[0].NodePath);
        Assert.Equal(1.0f, rules[0].AnchorRight);
        Assert.Equal(-200, rules[1].OffsetLeftPx);
    }

    [Fact]
    public void Match_GlobMatchesNestedPath()
    {
        var r = AnchorRules.Parse(SampleJson)[0];
        Assert.True(AnchorRules.Match(r.NodePath, "/root/Combat/HUD/TopBar"));
        Assert.True(AnchorRules.Match(r.NodePath, "/root/Map/HUD/TopBar"));
        Assert.False(AnchorRules.Match(r.NodePath, "/root/Combat/HUD/Other"));
    }

    [Fact]
    public void Parse_EmptyJson_ReturnsEmptyList()
    {
        var rules = AnchorRules.Parse("""{"rules": []}""");
        Assert.Empty(rules);
    }
}
```

- [ ] **Step 2: Run tests, confirm they fail**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release --filter FullyQualifiedName~AnchorRulesTests
```

Expected: failures with "AnchorRules does not exist".

- [ ] **Step 3: Implement AnchorRules.cs**

Create `src/SlayTheSpire2Ultrawide/AnchorRules.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlayTheSpire2Ultrawide;

public sealed class AnchorRule
{
    [JsonPropertyName("node_path")] public string NodePath { get; set; } = "";
    [JsonPropertyName("anchor_left")] public float AnchorLeft { get; set; }
    [JsonPropertyName("anchor_right")] public float AnchorRight { get; set; }
    [JsonPropertyName("anchor_top")] public float AnchorTop { get; set; }
    [JsonPropertyName("anchor_bottom")] public float AnchorBottom { get; set; }
    [JsonPropertyName("offset_left_px")] public int OffsetLeftPx { get; set; }
    [JsonPropertyName("offset_right_px")] public int OffsetRightPx { get; set; }
    [JsonPropertyName("offset_top_px")] public int OffsetTopPx { get; set; }
    [JsonPropertyName("offset_bottom_px")] public int OffsetBottomPx { get; set; }
}

internal sealed class AnchorRulesDoc
{
    [JsonPropertyName("rules")] public List<AnchorRule> Rules { get; set; } = new();
}

public static class AnchorRules
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static List<AnchorRule> Parse(string json)
    {
        var doc = JsonSerializer.Deserialize<AnchorRulesDoc>(json, Opts) ?? new AnchorRulesDoc();
        return doc.Rules;
    }

    public static bool Match(string pattern, string actualNodePath)
    {
        if (pattern.StartsWith("*/", StringComparison.Ordinal))
        {
            var suffix = pattern[2..];
            return actualNodePath == "/" + suffix || actualNodePath.EndsWith("/" + suffix, StringComparison.Ordinal);
        }
        return pattern == actualNodePath;
    }
}
```

- [ ] **Step 4: Run tests, confirm pass**

```powershell
dotnet test src/sts2-ultrawide.sln -c Release --filter FullyQualifiedName~AnchorRulesTests
```

Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/AnchorRules.cs src/SlayTheSpire2Ultrawide.Tests/AnchorRulesTests.cs
git commit -m "feat(anchors): AnchorRule schema + JSON parser + glob matcher"
```

---

### Task 10: Mod.cs entry point + scene-change hook

**Files:**
- Create: `src/SlayTheSpire2Ultrawide/Mod.cs`
- Delete: `src/SlayTheSpire2Ultrawide/Placeholder.cs`

This task wires the mod into Godot. The exact entry-point signature was learned in Task 2. The code below assumes Godot's standard pattern (`[GlobalClass]` / `Node` subclass auto-loaded). **Revise to match Task 2 findings before starting.**

- [ ] **Step 1: Delete placeholder**

```powershell
Remove-Item src/SlayTheSpire2Ultrawide/Placeholder.cs
```

- [ ] **Step 2: Implement Mod.cs**

Create `src/SlayTheSpire2Ultrawide/Mod.cs`:

```csharp
using Godot;

namespace SlayTheSpire2Ultrawide;

public partial class Mod : Node
{
    private Config _config = Config.Defaults();
    private ResolutionUnlocker? _resolution;
    private ViewportAdjuster? _viewport;
    private HudReanchorer? _hud;
    private BackgroundSwapper? _background;
    private DebugOverlay? _debug;

    public override void _Ready()
    {
        Log("sts2-ultrawide loading…");
        var configPath = GetConfigPath();
        _config = Config.LoadOrCreate(configPath);
        Log($"config: {configPath}");

        if (!_config.Enable)
        {
            Log("disabled via config; exiting");
            return;
        }

        _resolution = new ResolutionUnlocker(_config);
        _viewport = new ViewportAdjuster(_config);
        _hud = new HudReanchorer(_config, LoadAnchorRules());
        _background = new BackgroundSwapper(_config);
        if (_config.DebugOverlay) _debug = new DebugOverlay();

        _resolution.InjectIntoSettings();

        GetTree().NodeAdded += OnNodeAdded;
        GetTree().Root.SizeChanged += OnViewportSizeChanged;
        OnViewportSizeChanged();
    }

    private void OnViewportSizeChanged()
    {
        var size = GetViewport().GetVisibleRect().Size;
        Log($"viewport size: {size.X}x{size.Y}");
        _viewport?.Apply((int)size.X, (int)size.Y);
        _background?.Apply((int)size.X, (int)size.Y, GetTree().CurrentScene);
        _hud?.Apply(GetTree().CurrentScene);
        _debug?.Refresh(GetTree().Root);
    }

    private void OnNodeAdded(Node node)
    {
        _hud?.ApplyToSubtree(node);
        _background?.ApplyToSubtree(node, (int)GetViewport().GetVisibleRect().Size.X,
                                          (int)GetViewport().GetVisibleRect().Size.Y);
    }

    private List<AnchorRule> LoadAnchorRules()
    {
        const string resPath = "res://anchor_rules.json";
        if (!FileAccess.FileExists(resPath))
        {
            Log("anchor_rules.json missing from PCK; HUD reanchoring disabled");
            return new List<AnchorRule>();
        }
        using var f = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        return AnchorRules.Parse(f.GetAsText());
    }

    private string GetConfigPath()
    {
        var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var dir = System.IO.Path.GetDirectoryName(dllPath) ?? OS.GetExecutablePath();
        return System.IO.Path.Combine(dir, "config.toml");
    }

    private void Log(string msg)
    {
        if (_config.VerboseLog) GD.Print($"[sts2-ultrawide] {msg}");
        else GD.PrintRich($"[color=cyan][sts2-ultrawide][/color] {msg}");
    }
}
```

- [ ] **Step 3: Add stub classes so it compiles**

Create five throwaway files that we'll fill in over the next tasks:

`src/SlayTheSpire2Ultrawide/ResolutionUnlocker.cs`:
```csharp
namespace SlayTheSpire2Ultrawide;
internal sealed class ResolutionUnlocker
{
    public ResolutionUnlocker(Config _) { }
    public void InjectIntoSettings() { }
}
```

`src/SlayTheSpire2Ultrawide/ViewportAdjuster.cs`:
```csharp
namespace SlayTheSpire2Ultrawide;
internal sealed class ViewportAdjuster
{
    public ViewportAdjuster(Config _) { }
    public void Apply(int width, int height) { }
}
```

`src/SlayTheSpire2Ultrawide/HudReanchorer.cs`:
```csharp
using Godot;
namespace SlayTheSpire2Ultrawide;
internal sealed class HudReanchorer
{
    public HudReanchorer(Config _, List<AnchorRule> __) { }
    public void Apply(Node? scene) { }
    public void ApplyToSubtree(Node root) { }
}
```

`src/SlayTheSpire2Ultrawide/BackgroundSwapper.cs`:
```csharp
using Godot;
namespace SlayTheSpire2Ultrawide;
internal sealed class BackgroundSwapper
{
    public BackgroundSwapper(Config _) { }
    public void Apply(int width, int height, Node? scene) { }
    public void ApplyToSubtree(Node root, int width, int height) { }
}
```

`src/SlayTheSpire2Ultrawide/DebugOverlay.cs`:
```csharp
using Godot;
namespace SlayTheSpire2Ultrawide;
internal sealed class DebugOverlay
{
    public void Refresh(Node root) { }
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/
git rm src/SlayTheSpire2Ultrawide/Placeholder.cs
git commit -m "feat(mod): entry point + scene-change wiring with component stubs"
```

---

### Task 11: ResolutionUnlocker implementation

**Files:**
- Modify: `src/SlayTheSpire2Ultrawide/ResolutionUnlocker.cs`

The dropdown injection has to happen lazily — the settings scene only exists once the user opens the menu. Strategy: subscribe to `NodeAdded`, when we see a node matching `*/SettingsMenu/.../ResolutionDropdown` (the exact path comes from inspecting the game with the debug overlay or the decompiled scene files), append our entries to its `ItemList`.

- [ ] **Step 1: Implement ResolutionUnlocker.cs**

Replace `src/SlayTheSpire2Ultrawide/ResolutionUnlocker.cs`:

```csharp
using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class ResolutionUnlocker
{
    private readonly Config _config;
    private readonly HashSet<OptionButton> _injected = new();

    public ResolutionUnlocker(Config config) { _config = config; }

    public void InjectIntoSettings()
    {
        // Called once at mod boot. Real injection happens lazily as the settings
        // scene gets added to the tree — see OnNodeAdded.
    }

    public void OnNodeAdded(Node node)
    {
        if (node is not OptionButton ob) return;
        if (_injected.Contains(ob)) return;
        if (!LooksLikeResolutionDropdown(ob)) return;

        var screenSize = DisplayServer.ScreenGetSize();
        foreach (var (w, h) in AspectMath.ResolutionLadder())
        {
            if (w > screenSize.X || h > screenSize.Y) continue;
            var label = $"{w} x {h}";
            // Avoid duplicates if the game already lists it.
            var existing = false;
            for (int i = 0; i < ob.ItemCount; i++)
                if (ob.GetItemText(i) == label) { existing = true; break; }
            if (existing) continue;
            ob.AddItem(label);
            ob.SetItemMetadata(ob.ItemCount - 1, new Vector2I(w, h));
        }
        _injected.Add(ob);
        GD.PrintRich($"[color=cyan][sts2-ultrawide][/color] injected ultrawide entries into {ob.GetPath()}");
    }

    private static bool LooksLikeResolutionDropdown(OptionButton ob)
    {
        var path = ob.GetPath().ToString().ToLowerInvariant();
        // Heuristic — refine after inspecting the live scene with debug overlay.
        return path.Contains("resolution") || ob.Name.ToString().Contains("resolution", StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Wire it into Mod.cs**

Edit `src/SlayTheSpire2Ultrawide/Mod.cs` — find the `OnNodeAdded` method and add a call to the resolution unlocker:

```csharp
private void OnNodeAdded(Node node)
{
    _resolution?.OnNodeAdded(node);
    _hud?.ApplyToSubtree(node);
    _background?.ApplyToSubtree(node, (int)GetViewport().GetVisibleRect().Size.X,
                                      (int)GetViewport().GetVisibleRect().Size.Y);
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/ResolutionUnlocker.cs src/SlayTheSpire2Ultrawide/Mod.cs
git commit -m "feat(resolution): lazy-inject ultrawide entries into settings dropdown"
```

---

### Task 12: ViewportAdjuster implementation

**Files:**
- Modify: `src/SlayTheSpire2Ultrawide/ViewportAdjuster.cs`

- [ ] **Step 1: Implement ViewportAdjuster.cs**

Replace `src/SlayTheSpire2Ultrawide/ViewportAdjuster.cs`:

```csharp
using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class ViewportAdjuster
{
    private readonly Config _config;

    public ViewportAdjuster(Config config) { _config = config; }

    public void Apply(int width, int height)
    {
        if (!AspectMath.IsUltrawide(width, height)) return;
        var multiplier = AspectMath.CameraXMultiplier(width, height);

        // Make the root viewport expand horizontally rather than letterboxing.
        var window = (Window?)Engine.GetMainLoop().GetType().GetProperty("Root")?.GetValue(Engine.GetMainLoop());
        if (window is null) return;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleMode   = Window.ContentScaleModeEnum.CanvasItems;

        // Adjust active cameras: bump their horizontal extents by the multiplier.
        AdjustCamerasIn(window, (float)multiplier);
    }

    private static void AdjustCamerasIn(Node root, float multiplier)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Camera2D cam2d)
            {
                cam2d.Zoom = new Vector2(cam2d.Zoom.X / multiplier, cam2d.Zoom.Y);
            }
            AdjustCamerasIn(child, multiplier);
        }
    }
}
```

> Note: `Engine.GetMainLoop()` returning the `SceneTree` whose `Root` is the main `Window` is the standard Godot 4 path. If Task 2 found a tidier mod-loader-exposed `MainLoop` reference, prefer that.

- [ ] **Step 2: Build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/ViewportAdjuster.cs
git commit -m "feat(viewport): expand stretch mode and widen Camera2D zoom on ultrawide"
```

---

### Task 13: HudReanchorer implementation

**Files:**
- Modify: `src/SlayTheSpire2Ultrawide/HudReanchorer.cs`

- [ ] **Step 1: Implement HudReanchorer.cs**

Replace `src/SlayTheSpire2Ultrawide/HudReanchorer.cs`:

```csharp
using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class HudReanchorer
{
    private readonly Config _config;
    private readonly List<AnchorRule> _rules;
    private readonly HashSet<ulong> _applied = new();

    public HudReanchorer(Config config, List<AnchorRule> rules)
    {
        _config = config;
        _rules = rules;
    }

    public void Apply(Node? scene)
    {
        if (scene is null) return;
        Walk(scene);
    }

    public void ApplyToSubtree(Node root) => Walk(root);

    private void Walk(Node node)
    {
        if (node is Control ctl)
            ApplyMatchingRules(ctl);
        foreach (var child in node.GetChildren())
            Walk(child);
    }

    private void ApplyMatchingRules(Control ctl)
    {
        if (_applied.Contains(ctl.GetInstanceId())) return;
        var nodePath = ctl.GetPath().ToString();
        foreach (var rule in _rules)
        {
            if (!AnchorRules.Match(rule.NodePath, nodePath)) continue;

            ctl.AnchorLeft   = rule.AnchorLeft;
            ctl.AnchorRight  = rule.AnchorRight;
            ctl.AnchorTop    = rule.AnchorTop;
            ctl.AnchorBottom = rule.AnchorBottom;
            ctl.OffsetLeft   = rule.OffsetLeftPx;
            ctl.OffsetRight  = rule.OffsetRightPx;
            ctl.OffsetTop    = rule.OffsetTopPx;
            ctl.OffsetBottom = rule.OffsetBottomPx;

            _applied.Add(ctl.GetInstanceId());
            if (_config.VerboseLog)
                GD.Print($"[sts2-ultrawide] reanchored {nodePath} via {rule.NodePath}");
            break;
        }
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/HudReanchorer.cs
git commit -m "feat(hud): walk scene tree and apply anchor rules to Controls"
```

---

### Task 14: BackgroundSwapper implementation

**Files:**
- Modify: `src/SlayTheSpire2Ultrawide/BackgroundSwapper.cs`

- [ ] **Step 1: Implement BackgroundSwapper.cs**

Replace `src/SlayTheSpire2Ultrawide/BackgroundSwapper.cs`:

```csharp
using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class BackgroundSwapper
{
    private readonly Config _config;

    public BackgroundSwapper(Config config) { _config = config; }

    public void Apply(int width, int height, Node? scene)
    {
        if (scene is null) return;
        if (!AspectMath.IsUltrawide(width, height)) return;
        Walk(scene, width, height);
    }

    public void ApplyToSubtree(Node root, int width, int height)
    {
        if (!AspectMath.IsUltrawide(width, height)) return;
        Walk(root, width, height);
    }

    private void Walk(Node node, int width, int height)
    {
        if (node is Sprite2D sprite && IsBackground(sprite))
        {
            TrySwapTexture(sprite, width, height);
        }
        foreach (var child in node.GetChildren())
            Walk(child, width, height);
    }

    private static bool IsBackground(Sprite2D sprite)
    {
        var n = sprite.Name.ToString().ToLowerInvariant();
        return n.Contains("background") || n.Contains("bg") || sprite.GetParent()?.Name.ToString().ToLowerInvariant().Contains("background") == true;
    }

    private void TrySwapTexture(Sprite2D sprite, int width, int height)
    {
        // sceneKind drives which folder we look in. Coarse heuristic — refine per scene as anchors are.
        var sceneRoot = sprite.GetTree()?.CurrentScene?.Name.ToString().ToLowerInvariant() ?? "unknown";
        var kind = sceneRoot.Contains("combat") ? "combat"
                 : sceneRoot.Contains("map")    ? "map"
                 : sceneRoot.Contains("shop")   ? "shop"
                 : sceneRoot.Contains("event")  ? "event"
                 : sceneRoot.Contains("title")  ? "title"
                 : null;
        if (kind is null) return;

        var widest = PickPlate(kind, width);
        if (widest is null) return;

        var tex = ResourceLoader.Load<Texture2D>($"res://backgrounds/{kind}/{widest}.png");
        if (tex is null) return;
        sprite.Texture = tex;
        // Scale so plate width == viewport width; preserve aspect.
        var scaleX = (float)width / tex.GetWidth();
        sprite.Scale = new Vector2(scaleX, scaleX);
        if (_config.VerboseLog)
            GD.Print($"[sts2-ultrawide] swapped {sprite.GetPath()} → {kind}/{widest}.png");
    }

    private static string? PickPlate(string kind, int width)
    {
        // Pick the widest plate at or below the current viewport width.
        var available = new[] { 2560, 3440, 3840, 5120 };
        var best = available.Where(w => w <= width).OrderByDescending(w => w).FirstOrDefault();
        return best == 0 ? null : best.ToString();
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/BackgroundSwapper.cs
git commit -m "feat(background): swap Sprite2D backgrounds with widened plates from PCK"
```

---

### Task 15: DebugOverlay implementation

**Files:**
- Modify: `src/SlayTheSpire2Ultrawide/DebugOverlay.cs`

- [ ] **Step 1: Implement DebugOverlay.cs**

Replace `src/SlayTheSpire2Ultrawide/DebugOverlay.cs`:

```csharp
using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class DebugOverlay
{
    private CanvasLayer? _layer;
    private Label? _label;

    public void Refresh(Node root)
    {
        if (_layer is null)
        {
            _layer = new CanvasLayer { Layer = 128 };
            _label = new Label
            {
                Text = "sts2-ultrawide debug",
                Modulate = new Color(0, 1, 1, 0.9f),
                Position = new Vector2(8, 8),
            };
            _layer.AddChild(_label);
            root.AddChild(_layer);
        }
        var vp = _layer.GetViewport().GetVisibleRect().Size;
        _label!.Text = $"sts2-ultrawide  viewport {vp.X:0}x{vp.Y:0}  aspect {(vp.X / vp.Y):0.000}";
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/sts2-ultrawide.sln -c Release
```

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/SlayTheSpire2Ultrawide/DebugOverlay.cs
git commit -m "feat(debug): on-screen viewport/aspect overlay toggled by config"
```

---

## Phase 3 — Godot project & PCK assets

### Task 16: Minimal Godot project for PCK packing

**Files:**
- Create: `godot_project/project.godot`
- Create: `godot_project/export_presets.cfg`
- Create: `godot_project/assets/anchor_rules.json`
- Create: `godot_project/icon.svg`

We don't need a runnable game — just enough project shape that Godot's headless `--export-pack` will bundle our assets into a `.pck` the STS2 mod loader will read.

- [ ] **Step 1: Verify Godot is installed**

```powershell
godot --version
```

If absent, download Godot 4 (matching what STS2 ships — check `data_sts2_windows_x86_64` for hints, default to the latest 4.x stable). Set `GODOT_BIN` env var or add the binary to PATH.

- [ ] **Step 2: Create project.godot**

Create `godot_project/project.godot`:

```ini
config_version=5

[application]
config/name="sts2-ultrawide-pck"
config/features=PackedStringArray("4.3")
config/icon="res://icon.svg"

[rendering]
renderer/rendering_method="forward_plus"
```

- [ ] **Step 3: Create export_presets.cfg**

Create `godot_project/export_presets.cfg`:

```ini
[preset.0]

name="ModPCK"
platform="Windows Desktop"
runnable=false
custom_features=""
export_filter="all_resources"
include_filter=""
exclude_filter=""
export_path=""
encryption_include_filters=""
encryption_exclude_filters=""
encrypt_pck=false
encrypt_directory=false
script_export_mode=2

[preset.0.options]
```

- [ ] **Step 4: Add icon and starter anchor rules**

Drop in any 256x256 SVG (Godot's default `icon.svg` works) as `godot_project/icon.svg`. Then create `godot_project/assets/anchor_rules.json`:

```json
{
  "rules": [
    {
      "node_path": "*/HUD/TopBar",
      "anchor_left": 0.0, "anchor_right": 1.0,
      "anchor_top": 0.0, "anchor_bottom": 0.08,
      "offset_left_px": 24, "offset_right_px": -24,
      "offset_top_px": 0, "offset_bottom_px": 0
    },
    {
      "node_path": "*/HUD/EndTurnButton",
      "anchor_left": 1.0, "anchor_right": 1.0,
      "anchor_top": 0.85, "anchor_bottom": 1.0,
      "offset_left_px": -240, "offset_right_px": -24,
      "offset_top_px": 0, "offset_bottom_px": -24
    },
    {
      "node_path": "*/HUD/EnergyOrb",
      "anchor_left": 0.0, "anchor_right": 0.0,
      "anchor_top": 0.85, "anchor_bottom": 1.0,
      "offset_left_px": 24, "offset_right_px": 200,
      "offset_top_px": 0, "offset_bottom_px": -24
    },
    {
      "node_path": "*/HUD/DeckPile",
      "anchor_left": 0.0, "anchor_right": 0.0,
      "anchor_top": 0.85, "anchor_bottom": 1.0,
      "offset_left_px": 240, "offset_right_px": 380,
      "offset_top_px": 0, "offset_bottom_px": -24
    },
    {
      "node_path": "*/HUD/DiscardPile",
      "anchor_left": 1.0, "anchor_right": 1.0,
      "anchor_top": 0.85, "anchor_bottom": 1.0,
      "offset_left_px": -380, "offset_right_px": -240,
      "offset_top_px": 0, "offset_bottom_px": -24
    },
    {
      "node_path": "*/SettingsCog",
      "anchor_left": 1.0, "anchor_right": 1.0,
      "anchor_top": 0.0, "anchor_bottom": 0.0,
      "offset_left_px": -64, "offset_right_px": -24,
      "offset_top_px": 24, "offset_bottom_px": 64
    }
  ]
}
```

> **Important:** These node paths are starting guesses. Phase 4 manual smoke testing with `debug_overlay = true` is when you'll verify and correct them against the real STS2 scene tree.

- [ ] **Step 5: Headless export to verify**

```powershell
$out = "out/sts2-ultrawide.pck"
New-Item -ItemType Directory -Force out | Out-Null
godot --headless --path godot_project --export-pack "ModPCK" $out
```

Expected: `out/sts2-ultrawide.pck` created. Anchor rules are bundled at `res://assets/anchor_rules.json`. Update `Mod.cs` `LoadAnchorRules()` path from `res://anchor_rules.json` to `res://assets/anchor_rules.json`.

- [ ] **Step 6: Update Mod.cs anchor rules path**

Edit `src/SlayTheSpire2Ultrawide/Mod.cs`:

```csharp
private List<AnchorRule> LoadAnchorRules()
{
    const string resPath = "res://assets/anchor_rules.json";  // was res://anchor_rules.json
    if (!FileAccess.FileExists(resPath))
    {
        Log("anchor_rules.json missing from PCK; HUD reanchoring disabled");
        return new List<AnchorRule>();
    }
    using var f = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
    return AnchorRules.Parse(f.GetAsText());
}
```

Rebuild: `dotnet build src/sts2-ultrawide.sln -c Release`.

- [ ] **Step 7: Commit**

```bash
git add godot_project/ src/SlayTheSpire2Ultrawide/Mod.cs
git commit -m "feat(pck): minimal Godot project + starter anchor_rules.json + export preset"
```

---

### Task 17: One widened combat background plate (proof of swap mechanism)

**Files:**
- Create: `assets-src/backgrounds/combat/README.md`
- Create: `godot_project/assets/backgrounds/combat/3440.png`
- Create: `godot_project/assets/backgrounds/combat/5120.png`

Just **one** scene kind for v0.1.0 (combat). Other scene kinds either gracefully no-op or fall back to the game's original (stretched) art. Future versions add more.

- [ ] **Step 1: Extract a combat background from the game**

```powershell
./scripts/extract_game_assets.ps1
```

(If `gdsdecomp` isn't installed, see the script header for the download link.) Locate a combat scene background PNG under the extraction output. Copy the file into `assets-src/backgrounds/combat/original.png` for reference.

- [ ] **Step 2: Outpaint to widths 3440 and 5120**

Outpaint the original horizontally to 3440px wide (for 21:9 monitors) and 5120px wide (for 32:9). Use any tool — Photoshop generative fill, ComfyUI + an inpainting model, krita, GIMP resynthesizer. The vertical resolution should be the same proportional height (e.g. 3440x1440, 5120x1440).

Save the originals (with layers if any) under `assets-src/backgrounds/combat/3440.psd` and `5120.psd`. Export flattened PNGs into `godot_project/assets/backgrounds/combat/3440.png` and `5120.png`.

- [ ] **Step 3: Document the asset workflow**

Create `assets-src/backgrounds/combat/README.md`:

```markdown
# Combat background plates

Source: extracted from `SlayTheSpire2.pck` (v0.103.2). Original is roughly 1920x1080.

Variants:
- `3440.png` — 3440x1440, outpainted left/right to fill 21:9.
- `5120.png` — 5120x1440, outpainted further for 32:9.

Workflow when adding a new combat background or updating after a game patch:
1. Re-extract with `scripts/extract_game_assets.ps1`.
2. Replace `original.png`.
3. Outpaint horizontally (current method: <fill in once chosen>).
4. Export to PNG into `godot_project/assets/backgrounds/combat/`.
5. Re-export the PCK via `scripts/build.ps1`.

Style notes: keep edge seams smooth, preserve color grade and depth-of-field. The center 1920px should be visually indistinguishable from the original.
```

- [ ] **Step 4: Rebuild PCK and verify assets are bundled**

```powershell
./scripts/build.ps1
```

Inspect the resulting PCK if curious:

```powershell
godot --headless --path godot_project --quit-after 1 2>&1 | Select-String -Pattern "loaded"
```

Or just trust the export; we'll verify visually in Phase 4.

- [ ] **Step 5: Commit assets**

```bash
git add assets-src/backgrounds/combat/README.md godot_project/assets/backgrounds/combat/
git commit -m "feat(art): combat background plates at 3440 and 5120 widths"
```

> **PSD/source-of-truth note:** `assets-src/backgrounds/combat/*.psd` files can be large. If they exceed 50 MB, gitignore them and store separately (e.g. in a Drive folder linked from the README). For v0.1.0, commit if reasonable size.

---

## Phase 4 — Local validation

### Task 18: Baseline smoke test at 1920x1080 (no-regression)

**Files:** none (manual test pass)

- [ ] **Step 1: Build and install**

```powershell
./scripts/build.ps1
./scripts/install.ps1
```

- [ ] **Step 2: Run game at 1920x1080**

Launch `SlayTheSpire2.exe`. Set monitor to 1920x1080 fullscreen.

Verify:
- "Modded" banner appears.
- Game looks identical to vanilla at this resolution (no anchor rules should trigger because `IsUltrawide()` returns false).
- Cards, HUD, backgrounds unchanged.
- Start a combat — no crashes; HUD elements where the vanilla game put them.

If anything looks different at 16:9, the mod is incorrectly intervening on non-ultrawide. Add an early-return in the relevant component (`HudReanchorer.Apply` and `BackgroundSwapper.Apply` already gate on aspect ratio; `Mod.cs` should not call them when `!IsUltrawide`).

- [ ] **Step 3: Capture baseline screenshot**

Save a screenshot of the main menu and a combat scene at 1920x1080 to `docs/screenshots/baseline-1080p-mainmenu.png` and `baseline-1080p-combat.png`. These are the regression reference for v0.1.0.

- [ ] **Step 4: Commit screenshots**

```bash
git add docs/screenshots/
git commit -m "test: baseline 1080p reference screenshots"
```

---

### Task 19: Primary target — smoke test at 5120x1440

**Files:** `docs/screenshots/5120x1440-*.png`, possibly `godot_project/assets/anchor_rules.json` edits

- [ ] **Step 1: Set monitor to 5120x1440 and enable debug overlay**

Edit `mods/sts2-ultrawide/config.toml` (generated on first run, or write manually):

```toml
enable = true
force_aspect = "auto"
hud_edge_padding_px = 24
card_hand_max_spread_px = 1400
debug_overlay = true
verbose_log = true
```

- [ ] **Step 2: Launch and verify resolution unlock**

Open Settings → Video. Verify `5120 x 1440` and other ladder entries appear in the dropdown. Select `5120 x 1440`. Confirm the game switches resolution without crashing.

- [ ] **Step 3: Title screen check**

Verify the debug overlay shows `viewport 5120x1440 aspect 3.556`. Check the title screen: background should extend wider than 16:9 (will fall back to stretched original for v0.1.0 since we only ship combat backgrounds — note this as expected for v0.1).

- [ ] **Step 4: Combat scene check (the big one)**

Start any combat. Verify:
- Combat background uses the widened 5120 plate (visually clear at the seams).
- Top bar spans the full width.
- End-turn button hugs the bottom-right corner.
- Energy orb hugs the bottom-left corner.
- Deck and discard piles sit between energy orb and end-turn button.
- Card hand stays centered with reasonable spread.

For any HUD element that's in the wrong place: read its actual node path from the verbose log (it'll show every reanchor attempt). Update the corresponding rule in `godot_project/assets/anchor_rules.json`. Rebuild PCK with `./scripts/build.ps1`, re-`install.ps1`, restart game, recheck. Iterate until correct.

- [ ] **Step 5: Map, event, shop check**

Open the map. Verify it's centered or extended cleanly. Trigger an event. Visit a shop. Note any unanchored HUD elements (settings cog, gold display) and add rules for them.

- [ ] **Step 6: Capture screenshots and turn off debug overlay**

Screenshot main menu, combat, map, shop, event. Save under `docs/screenshots/5120x1440-*.png`. Edit `config.toml` to set `debug_overlay = false` and `verbose_log = false`.

- [ ] **Step 7: Commit final anchor rules + screenshots**

```bash
git add godot_project/assets/anchor_rules.json docs/screenshots/
git commit -m "test(5120x1440): finalize anchor rules and capture validation screenshots"
```

---

### Task 20: Smoke test at 3440x1440 and 2560x1080

**Files:** possibly `godot_project/assets/anchor_rules.json` edits, screenshots

- [ ] **Step 1: Test 3440x1440**

Set the monitor (or use Windowed mode at 3440x1440 if your panel can't display it natively). Repeat Task 19 step 4 checks. The same anchor rules should work — the difference is just that backgrounds will use the 3440 plate.

Capture `docs/screenshots/3440x1440-combat.png`.

- [ ] **Step 2: Test 2560x1080**

Same drill. Verify the dropdown still offers this, the 5120 plate isn't selected (BackgroundSwapper.PickPlate should pick 2560 or fall through), and HUD anchors work.

Capture `docs/screenshots/2560x1080-combat.png`.

- [ ] **Step 3: Note any aspect-specific glitches**

If a rule that works at 32:9 breaks at 21:9 (e.g. an offset that's right for one but wrong for another), document this in the README's "Known issues" section and either accept it for v0.1 or add per-aspect rule support (deferred — out of v0.1 scope, document instead).

- [ ] **Step 4: Commit**

```bash
git add docs/screenshots/ godot_project/assets/anchor_rules.json README.md
git commit -m "test(21:9): validate 3440x1440 and 2560x1080 against same anchor rules"
```

---

## Phase 5 — GitHub & Nexus pipeline

### Task 21: Create GitHub repo and push

**Files:** none (remote-only changes)

- [ ] **Step 1: Create the public repo**

```powershell
gh repo create MohamedSerhan/sts2-ultrawide `
    --public `
    --description "Ultrawide resolution support mod for Slay the Spire 2 (up to 5120x1440)" `
    --homepage "https://www.nexusmods.com/slaythespire2/mods/" `
    --source . `
    --remote origin `
    --push
```

Expected: repo exists at `https://github.com/MohamedSerhan/sts2-ultrawide`, all local commits visible.

- [ ] **Step 2: Verify**

```powershell
gh repo view MohamedSerhan/sts2-ultrawide --web
```

(Or check in browser.) README renders, screenshots visible.

- [ ] **Step 3: No commit needed — push happens above.**

---

### Task 22: CI workflow — build on push

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Write ci.yml**

Create `.github/workflows/ci.yml`:

```yaml
name: ci

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup Godot
        uses: chickensoft-games/setup-godot@v2
        with:
          version: 4.3.0
          use-dotnet: false

      - name: Build DLL
        run: dotnet build src/sts2-ultrawide.sln -c Release

      - name: Run tests
        run: dotnet test src/sts2-ultrawide.sln -c Release --no-build --verbosity normal

      - name: Export PCK
        shell: pwsh
        run: |
          New-Item -ItemType Directory -Force out | Out-Null
          godot --headless --path godot_project --export-pack "ModPCK" out/sts2-ultrawide.pck

      - name: Package
        shell: pwsh
        run: ./scripts/package.ps1

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: sts2-ultrawide-zip
          path: out/sts2-ultrawide-*.zip
```

> Note: `lib/GodotSharp.dll` is gitignored, so CI needs a way to get it. Two options: (1) commit a minimal stripped GodotSharp.dll under `vendor/` for CI use only (check Godot's license — MIT, allowed); (2) have the workflow download Godot's editor zip and extract GodotSharp.dll. Option 1 is simpler. Add the file to repo as `vendor/GodotSharp.dll` and update `SlayTheSpire2Ultrawide.csproj` HintPath to `..\..\vendor\GodotSharp.dll` (or copy to `src/lib/` in a CI prep step). Implement Option 1 by adding a "Restore vendor" step that copies `vendor/GodotSharp.dll` into `src/lib/` before `dotnet build`.

- [ ] **Step 2: Add the vendor GodotSharp.dll**

```powershell
mkdir -Force vendor
Copy-Item "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/GodotSharp.dll" vendor/
```

Verify it's the same as the one the game ships (sha256). Update `.gitignore` to remove the `src/lib/*.dll` block *only if* you switch the csproj to point at `vendor/`. Simplest: keep csproj pointing at `src/lib/`, and have ci.yml copy `vendor/GodotSharp.dll` → `src/lib/` before build:

Edit `.github/workflows/ci.yml`, add before "Build DLL":

```yaml
      - name: Stage vendored Godot DLLs
        shell: pwsh
        run: |
          New-Item -ItemType Directory -Force src/lib | Out-Null
          Copy-Item vendor/GodotSharp.dll src/lib/
```

- [ ] **Step 3: Commit and push**

```bash
git add .github/workflows/ci.yml vendor/GodotSharp.dll
git commit -m "ci: build, test, export PCK, and package on every push"
git push origin main
```

- [ ] **Step 4: Watch first CI run**

```powershell
gh run watch
```

Expected: green build, artifact `sts2-ultrawide-zip` attached to the run. If red, fix and push again.

---

### Task 23: Release workflow + Nexus upload

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Manually create the Nexus mod page (one-time, by hand)**

Go to `https://www.nexusmods.com/slaythespire2/mods/add`. Fill in:
- Title: "Ultrawide Support"
- Description: a marketing-cleanup version of the README's first two paragraphs
- Category: User Interface / Display
- Tags: ultrawide, 32:9, 21:9, resolution
- Upload a placeholder file (or the locally-built v0.0.1 zip just to satisfy the page requirement).

Note the **mod ID** in the URL (e.g. `/slaythespire2/mods/123`). That's `NEXUS_MOD_ID`. The game slug `slaythespire2` is `NEXUS_GAME_ID`.

- [ ] **Step 2: Generate a Nexus personal API key**

`https://www.nexusmods.com/users/myaccount?tab=api+access` → "Generate" a personal API key. Copy the value.

- [ ] **Step 3: Add secrets to the GitHub repo**

```powershell
gh secret set NEXUS_API_KEY --body "<paste-key>"
gh secret set NEXUS_GAME_ID --body "slaythespire2"
gh secret set NEXUS_MOD_ID  --body "<numeric id>"
```

- [ ] **Step 4: Write release.yml**

Create `.github/workflows/release.yml`:

```yaml
name: release

on:
  push:
    tags: ['v*']

permissions:
  contents: write

jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup Godot
        uses: chickensoft-games/setup-godot@v2
        with:
          version: 4.3.0
          use-dotnet: false

      - name: Stage vendored Godot DLLs
        shell: pwsh
        run: |
          New-Item -ItemType Directory -Force src/lib | Out-Null
          Copy-Item vendor/GodotSharp.dll src/lib/

      - name: Build
        run: dotnet build src/sts2-ultrawide.sln -c Release

      - name: Test
        run: dotnet test src/sts2-ultrawide.sln -c Release --no-build

      - name: Export PCK
        shell: pwsh
        run: |
          New-Item -ItemType Directory -Force out | Out-Null
          godot --headless --path godot_project --export-pack "ModPCK" out/sts2-ultrawide.pck

      - name: Package
        shell: pwsh
        run: ./scripts/package.ps1

      - name: Derive version
        id: ver
        shell: pwsh
        run: |
          $v = "${{ github.ref_name }}".TrimStart('v')
          "version=$v" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: out/sts2-ultrawide-*.zip
          generate_release_notes: true

      - name: Upload to Nexus Mods
        uses: Nexus-Mods/upload-action@v1
        with:
          api-key:       ${{ secrets.NEXUS_API_KEY }}
          game-domain:   ${{ secrets.NEXUS_GAME_ID }}
          mod-id:        ${{ secrets.NEXUS_MOD_ID }}
          version:       ${{ steps.ver.outputs.version }}
          file-path:     out/sts2-ultrawide-${{ steps.ver.outputs.version }}.zip
          file-name:     "sts2-ultrawide-${{ steps.ver.outputs.version }}.zip"
          file-description: "v${{ steps.ver.outputs.version }} — see GitHub release for changelog."
```

> Verify the exact input names against the upstream action's README before pushing. The Nexus action is the official one; if the inputs differ from above, adjust. The action only updates the *file*; the page's title/description/category remain manually managed.

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "ci: release workflow — GitHub release + Nexus file upload on tag"
git push origin main
```

---

### Task 24: Cut v0.1.0 release

**Files:** none

- [ ] **Step 1: Final pre-release checklist**

- All tests pass locally: `dotnet test src/sts2-ultrawide.sln -c Release`.
- `mod_manifest.json` version is `0.1.0`.
- Screenshots in README look right and link correctly.
- "Known issues" section in README is honest (e.g. "v0.1 ships only combat backgrounds; map/event/shop fall back to stretched 16:9 art").

- [ ] **Step 2: Tag and push**

```bash
git tag -a v0.1.0 -m "v0.1.0: initial ultrawide support — resolutions, HUD anchoring, combat backgrounds"
git push origin v0.1.0
```

- [ ] **Step 3: Watch the release workflow**

```powershell
gh run watch
```

Expected: green build, GitHub release published at `https://github.com/MohamedSerhan/sts2-ultrawide/releases/tag/v0.1.0`, file appears on the Nexus mod page.

- [ ] **Step 4: Verify Nexus page**

Open the Nexus mod page in a browser. Confirm the new v0.1.0 file is listed. Manually fix the changelog/description on Nexus if the auto-upload's `file-description` isn't enough.

- [ ] **Step 5: Final commit (CHANGELOG)**

Create `CHANGELOG.md`:

```markdown
# Changelog

## v0.1.0 — 2026-05-17

Initial release.

### Added
- Ultrawide resolution entries in the in-game settings dropdown: 2560x1080, 3440x1440, 3840x1600, 5120x1440, 5120x2160.
- Viewport stretch mode set to expand, with Camera2D zoom widened proportional to aspect ratio.
- HUD re-anchoring for top bar, end-turn button, energy orb, deck/discard piles, settings cog.
- Widened combat-scene background plates at 3440 and 5120 widths.
- `config.toml` with master switch, force-aspect override, HUD padding, card spread cap, and a debug overlay toggle.
- Public GitHub repo and automated release pipeline pushing to GitHub Releases + Nexus Mods.

### Known limitations
- Only combat backgrounds ship widened plates in v0.1. Map, shop, and event backgrounds fall back to the vanilla 16:9 art (will be stretched or letterboxed depending on the scene).
- HUD anchor rules tuned against STS2 v0.103.2. Future game patches that rename scene nodes will require an updated `anchor_rules.json`.
- Multiplayer with mixed-aspect-ratio players is untested.
```

```bash
git add CHANGELOG.md
git commit -m "docs: v0.1.0 changelog"
git push origin main
```

---

## Self-review notes (filled in by the planner; informational for executors)

**Spec coverage check:** Spec sections mapped to tasks:
- "Goals" (5 bullets) → Tasks 11, 12, 13, 14, 16-20 collectively.
- "Architecture" hybrid DLL+PCK → Tasks 3-15 (DLL) + 16-17 (PCK).
- "Components 1-6" → Tasks 11 (resolution), 12 (viewport), 13 (hud), 14 (background), 7 (config), 15 (debug overlay), 5 (manifest).
- "Resolution ladder" → Task 8 (math) + Task 19/20 (tests).
- "Repository layout" → Tasks 1-6, 16, 22-23.
- "Build & release pipeline" → Tasks 6, 22, 23, 24.
- "Testing strategy" → Tasks 7, 8, 9 (unit tests); 18-20 (manual smoke).
- "Risks" → addressed in code (PCK-only patchability of `anchor_rules.json`), in README ("Known issues" Task 24), and through `compat-target.txt` (Task 5).

**Placeholder scan:** Confirmed no "TBD"/"TODO"/"add appropriate" placeholders in concrete steps. Two explicit "discovery → revise later" callouts in Task 2 and Task 10 are intentional and bounded — they identify which downstream tasks to revisit if assumptions break.

**Type consistency:** `Config`, `AspectMath`, `AnchorRules`/`AnchorRule`, `Mod`, `ResolutionUnlocker`, `ViewportAdjuster`, `HudReanchorer`, `BackgroundSwapper`, `DebugOverlay` — all defined in Task 7-15, all references match. Method signatures consistent across stub (Task 10) and implementation (Tasks 11-15).

**Scope check:** Single mod, single feature, single deployment pipeline. Sized appropriately for one plan.
