# STS2 Ultrawide Mod — Design

**Status:** Draft for review
**Date:** 2026-05-17
**Target game version:** Slay the Spire 2 `v0.103.2` (commit `89765e1e`, 2026-04-16)

## Purpose

Slay the Spire 2 (Godot 4, Mega Crit) doesn't expose ultrawide resolutions in its resolution dropdown. A user on a 32:9 5120x1440 panel either runs windowed at 16:9 with empty desktop on the sides, or sets the OS to scale and gets a distorted picture. This mod fills that gap: it unlocks ultrawide resolutions, extends the rendered scene to fill the screen, and reflows the HUD to the true viewport edges so the game looks native at the panel's full width.

## Goals

- Run STS2 cleanly at the common ultrawide ladder: **2560x1080, 3440x1440, 3840x1600, 5120x1440, 5120x2160**.
- Backgrounds and battlefield art extend to fill the width (no horizontal stretching of 16:9 plates).
- HUD elements (top bar, end-turn button, energy, deck/discard piles, map controls) anchor to the *true* left/right edges of the viewport, not the 16:9 center.
- Card hand stays centered with sensible spread; map and event screens reflow within the wider viewport.
- Ship through STS2's built-in mod loader: `mods/sts2-ultrawide/{sts2-ultrawide.dll, sts2-ultrawide.pck, mod_manifest.json}`.
- Public GitHub repo, MIT licensed, with CI that produces a Nexus-ready release zip on every version tag and uses the official `Nexus-Mods/upload-action` to push the file to an existing Nexus mod page.

## Non-goals

- Multi-monitor / surround-style 48:9 layouts. Single-display ultrawides only.
- Aspect-ratio support outside the common ultrawide range (no 4:3, 5:4, 1:1, 9:16 portrait).
- Gameplay changes of any kind. `affects_gameplay: false` in the manifest.
- Asset overhauls or higher-resolution textures of existing 16:9 content. We only ship widened *background plates* + decorative side panels where 16:9 art can't be extended.
- Localization changes, controller remapping, FOV beyond what aspect ratio dictates.
- Compatibility shims for every other mod. We coexist with passive mods; conflicts with mods that also patch UI scenes are documented, not engineered around.

## Architecture

Hybrid mod: one .NET DLL drives runtime behavior, one PCK ships widened art assets. Both load through STS2's built-in mod loader.

```
sts2-ultrawide/
├── mod_manifest.json     ← schema matches what installed mods use (id, pck_name, has_pck, has_dll, dependencies, min_game_version)
├── sts2-ultrawide.dll    ← C# runtime patches
├── sts2-ultrawide.pck    ← widened background plates + side panels
└── config.toml           ← user-tunable (lives next to DLL; written on first run if missing)
```

The DLL is the brain; the PCK is a passive asset bank the DLL pulls from.

### Component 1 — Resolution unlocker (DLL)

On boot, the DLL hooks the settings/video screen and injects the supported ultrawide entries into the resolution dropdown. The list is filtered by `DisplayServer.screen_get_size()` so users only see resolutions their primary display can actually run. Selecting one writes through to STS2's existing settings storage so it persists.

### Component 2 — Viewport / camera adjuster (DLL)

When the active resolution's aspect ratio differs from 16:9, the DLL:
- Switches the root viewport's stretch mode to `viewport`/`expand` (so the world widens instead of letterboxing).
- For combat scenes: increases the orthographic camera's horizontal extent by `currentAspect / 1.7778`. Vertical extent unchanged.
- For map / event / shop scenes: same horizontal extent boost; verifies bounds against the level's playable region.

### Component 3 — HUD re-anchorer (DLL)

The biggest piece. On every scene change:
1. Walk the loaded scene tree from the root.
2. Match nodes by stable path/name against a small `AnchorRules` table (top bar, energy orb, end-turn button, deck pile, discard pile, map zoom controls, settings cog, …).
3. For each matched node, override its anchor preset so it tracks the true viewport edge (left → `anchor_left = 0`, right → `anchor_right = 1`) instead of being baked to a 1920px-wide canvas.
4. Card hand container: leave centered, but bump `separation` and `max_width` so cards spread to use the extra space without overlapping.

The `AnchorRules` table is data, not code — it lives in a JSON file inside the PCK so we can patch it for new game versions without recompiling the DLL.

### Component 4 — Background plate swapper (DLL + PCK)

For each "scene group" (combat backgrounds, map vista, shop interior, event scenes, title screen), the mod ships widened plates in the PCK at known paths. When a scene loads, the DLL checks `currentAspect` and, if > 16:9, swaps the relevant `Sprite2D.texture` to the widest plate that's ≤ the current aspect, then scales to exact viewport width. Side decorative panels (spire silhouettes) are positioned past the original art bounds so backgrounds don't end in a hard cut.

### Component 5 — Config (DLL)

`config.toml` is generated on first run. Keys:
- `enable = true` — master switch
- `force_aspect = "auto"` — `auto` | one of the ladder values, lets users override detection
- `hud_edge_padding_px = 24` — visual breathing room from viewport edges
- `card_hand_max_spread_px = 1400` — cap on how wide the hand fans on 32:9
- `debug_overlay = false` — draws viewport rect + anchor markers on screen
- `verbose_log = false`

### Component 6 — Manifest

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

A `compat-target.txt` next to the manifest pins `0.103.2`, matching what `STS2-RitsuLib` ships, so users can see at a glance what game build this version was tested against.

## Data flow

```
Game boot
  └─> mod loader scans /mods → loads our DLL + PCK
        └─> DLL hooks SceneTree.tree_changed (Godot signal)
             └─> on settings scene: inject ultrawide entries into resolution dropdown
             └─> on any scene change:
                   ├─> read currentAspect
                   ├─> if > 16:9: adjust camera extents
                   ├─> walk node tree, apply AnchorRules
                   └─> swap background Sprite2D textures from our PCK
```

## Resolution ladder & layout anchors

| Resolution  | Aspect | Camera-x mult | Notes                                     |
|-------------|--------|---------------|-------------------------------------------|
| 1920x1080   | 1.778  | 1.000         | Untouched baseline                        |
| 2560x1080   | 2.370  | 1.333         | 21:9 entry-level                          |
| 3440x1440   | 2.389  | 1.344         | 21:9 standard                             |
| 3840x1600   | 2.400  | 1.350         | 24:10                                     |
| 5120x1440   | 3.556  | 2.000         | 32:9 target panel — primary test surface  |
| 5120x2160   | 2.370  | 1.333         | 21:9 5K                                   |

Layouts interpolate between these via the camera-x multiplier; the anchor rules apply identically.

## Repository layout

```
sts2-ultrawide/
├── src/                          # C# sources for the DLL
│   ├── Mod.cs                    # entry point, scene-change hook
│   ├── ResolutionUnlocker.cs
│   ├── ViewportAdjuster.cs
│   ├── HudReanchorer.cs
│   ├── BackgroundSwapper.cs
│   └── Config.cs
├── assets/                       # source assets for the PCK
│   ├── backgrounds/<scene>/<width>.png
│   ├── side_panels/spire_left.png
│   ├── side_panels/spire_right.png
│   └── anchor_rules.json
├── godot_project/                # minimal Godot 4 project used to bake assets into a PCK
│   └── project.godot
├── scripts/
│   ├── build.ps1                 # local build: dotnet build + Godot --export-pack
│   ├── package.ps1               # zips the mod into a Nexus-ready release artifact
│   └── install.ps1               # copies the built mod into the live STS2 install for testing
├── .github/workflows/
│   ├── ci.yml                    # build on every push
│   └── release.yml               # on tag: build, package, gh release, Nexus upload
├── docs/
│   └── superpowers/specs/2026-05-17-ultrawide-mod-design.md
├── mod_manifest.json
├── compat-target.txt
├── README.md
├── LICENSE                       # MIT
└── .gitignore
```

## Build & release pipeline

**Local (Windows, PowerShell):**
1. `scripts/build.ps1` — restore + build the C# project (`dotnet publish -c Release`) and run Godot headless to export `sts2-ultrawide.pck` from `godot_project/`.
2. `scripts/package.ps1` — assemble a folder matching the install layout, zip it as `sts2-ultrawide-<version>.zip`.
3. `scripts/install.ps1` — copy that folder into `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\sts2-ultrawide` for live testing. Idempotent.

**CI (GitHub Actions):**
- `ci.yml` — on every push: dotnet build + Godot export, fail on warnings, upload zip as workflow artifact.
- `release.yml` — on `v*` tag:
  1. Build + package.
  2. `gh release create` with the zip attached.
  3. `Nexus-Mods/upload-action@v1` uploads the same zip as a new file version on an existing Nexus mod page. Requires three repo secrets: `NEXUS_API_KEY`, `NEXUS_GAME_ID` (slay-the-spire-2's slug), `NEXUS_MOD_ID` (numeric, from the URL after first manual page creation). The action only updates the *file*; description, changelog summary, and category stay manually managed on Nexus.

The mod page itself has to be created by hand once on nexusmods.com before CI can push to it.

## Testing strategy

**Manual smoke (primary, every change):**
1. Run `install.ps1`. Launch `SlayTheSpire2.exe`.
2. Verify "Modded" banner appears and ultrawide entries show in the resolution list.
3. Set 5120x1440. Verify: title screen background fills width; map screen pans correctly; combat HUD is at the true edges; cards fan without overlap; an event screen renders without text clipping.
4. Repeat at 3440x1440 and 2560x1080.

**Debug overlay:**
Toggling `debug_overlay = true` in `config.toml` draws the viewport rect plus a marker on every node the HUD re-anchorer modifies. Used to diagnose new-scene coverage and spot regressions after game updates.

**Automated (CI):**
- DLL unit tests for `Config` parsing and the aspect-ratio → camera-multiplier math.
- A "scene walk" smoke test: load a saved Godot scene fixture, run the re-anchorer, assert anchors match expected values. Lives in `src/Tests/`.
- No end-to-end test in CI — running STS2 headless on a runner is out of scope.

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| STS2 patches rename UI node paths → re-anchorer misses targets | `AnchorRules` is JSON inside the PCK, ship a quick PCK-only patch release. `debug_overlay` makes regressions visible to users who can then file an issue. |
| Some scenes are baked CanvasLayers with hardcoded sizes that ignore viewport changes | Detected during manual smoke; fall back to that scene's PCK replacement (widened plate) without re-anchoring, or pillarbox just that scene. |
| Background art can't be cleanly extended (e.g. a centered statue) | Ship a decorative side panel as filler rather than awkwardly stretching the original. Per-scene call. |
| Multiplayer desyncs from clients running different aspect ratios | `affects_gameplay = false` — we only touch presentation. If multiplayer matchmaking still flags us as modded, document the limitation; don't engineer around it. |
| Nexus upload-action breaks or API key leaks | Action is pinned by major version; key is a repo secret with `mod_management:upload` scope only. Manual upload always remains as fallback. |
| Game version churn during early access invalidates `compat-target.txt` | Pin per release; bump `min_game_version` on each STS2 update; ship a new mod version. Pinning is honest: users see exactly what build it was tested on. |

## Open questions for review

1. **Mod ID and Nexus page name.** Spec uses `sts2-ultrawide` and "Ultrawide Support" — fine, or do you want something more branded (e.g. "TrueWide", "Spire on Wide")?
2. **License.** MIT assumed. Confirm.
3. **C# project SDK target.** Default `net8.0` to match what STS2's other mod DLLs likely target — confirm during scaffolding by inspecting an existing mod's DLL with `ildasm`/`ilspy`.
4. **Asset sourcing.** Widened background plates require either AI outpainting, manual extension in an image editor, or extracting + extending the originals from `SlayTheSpire2.pck`. Plan default: extract originals, outpaint horizontally, hand-clean obvious seams. Confirm this is acceptable vs. shipping pillarbox-with-side-art as a fallback for scenes that can't be cleanly extended.
