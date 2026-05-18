# Combat background plates

Source: extract from `SlayTheSpire2.pck` using `scripts/extract_game_assets.ps1` (requires [`gdsdecomp`](https://github.com/bruvzg/gdsdecomp/releases)). The original combat-scene background plate is roughly 1920x1080.

## Variants we ship in the PCK

Drop the outpainted PNGs into `godot_project/assets/backgrounds/combat/` with these exact filenames:

- `3440.png` — 3440x1440 (21:9 standard)
- `5120.png` — 5120x1440 (32:9, the primary 5120x1440 target)

Optional rungs (the BackgroundSwapper picks the widest plate at or below the current viewport width):

- `2560.png` — 2560x1080
- `3840.png` — 3840x1600

## Outpainting workflow

1. Re-extract with `scripts/extract_game_assets.ps1` and copy the original here as `original.png`.
2. Outpaint horizontally to the target widths. Any tool works — Photoshop generative fill, ComfyUI + an inpainting model, krita, or GIMP resynthesizer. Keep the center 1920px visually identical to the original; only extend.
3. Match the original's color grade and depth-of-field on the new edge regions.
4. Save full-res working files here (`3440.psd`, `5120.psd`) if they fit under ~50 MB; otherwise store separately and link from this README.
5. Export flattened PNGs into `godot_project/assets/backgrounds/combat/`.
6. Rebuild with `scripts/build.ps1` — the PCK picks them up automatically.

## v0.1.0 status

No plates ship in v0.1.0. The BackgroundSwapper looks for them, falls through silently when they're absent, and the vanilla 16:9 art is left in place (stretched or whatever the underlying scene does). Adding plates is the first follow-up release.
