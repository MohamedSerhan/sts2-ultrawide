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
