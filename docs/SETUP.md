# Setup — CI / Nexus release pipeline

The CI and release workflows need three repo secrets to compile (vendored game DLLs, base64-encoded) and three more to push to Nexus Mods. This is a one-time setup per repo.

## 1. Vendor the game DLLs as secrets

The mod references three DLLs that ship with Slay the Spire 2:
- `GodotSharp.dll` — Godot C# bindings (MIT)
- `sts2.dll` — Mega Crit's game assembly (proprietary — that's why we don't commit it)
- `0Harmony.dll` — HarmonyLib (MIT)

Encode each one and set as a repo secret:

```powershell
$root = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64"
gh secret set VENDOR_GODOTSHARP_DLL_B64 --body ([Convert]::ToBase64String([IO.File]::ReadAllBytes("$root\GodotSharp.dll")))
gh secret set VENDOR_STS2_DLL_B64       --body ([Convert]::ToBase64String([IO.File]::ReadAllBytes("$root\sts2.dll")))
gh secret set VENDOR_HARMONY_DLL_B64    --body ([Convert]::ToBase64String([IO.File]::ReadAllBytes("$root\0Harmony.dll")))
```

Each DLL is a few MB; well under the 48 KB-encrypted-secret cap when base64-encoded? **No** — GitHub's secret value cap is 48 KB. `sts2.dll` is ~9 MB → 12 MB base64 → won't fit.

**Workaround:** store the DLLs as a release artifact on a *private* mirror repo, and have CI download them. Or, simpler:

```powershell
# Create a private "vendor" repo just for the DLLs.
gh repo create MohamedSerhan/sts2-ultrawide-vendor --private
$tmp = mkdir vendor-tmp -Force
Copy-Item "$root\GodotSharp.dll","$root\sts2.dll","$root\0Harmony.dll" $tmp
cd vendor-tmp; git init -b main; git add .; git commit -m "snapshot v0.103.2"
git remote add origin https://github.com/MohamedSerhan/sts2-ultrawide-vendor.git
git push -u origin main
cd ..

# Add a fine-grained PAT with `Contents: read` on the vendor repo as a secret.
gh secret set VENDOR_REPO_PAT --body "<paste-token>"
```

Then update the workflow's restore step to `gh api repos/MohamedSerhan/sts2-ultrawide-vendor/contents/<dll> -H "Accept: application/vnd.github.raw"`.

The current workflow assumes the 48 KB-fits path. If you go the private-mirror route, edit the "Restore vendored game DLLs" step accordingly.

## 2. Create the Nexus mod page (one-time, manual)

The Nexus upload API only updates *files* on existing mod pages. The initial page has to be created by hand:

1. Sign in at nexusmods.com.
2. Visit `https://www.nexusmods.com/slaythespire2/mods/add` (or the equivalent submission URL).
3. Fill in:
   - Title: `Ultrawide Support`
   - Category: User Interface / Display
   - Description: marketing copy adapted from the README's first paragraph.
   - Tags: `ultrawide, 32:9, 21:9, resolution`
   - Initial file: upload `out/sts2-ultrawide-0.1.0.zip` once locally.
4. Note the mod's numeric ID in the URL: `/slaythespire2/mods/<NNNN>`. That's `NEXUS_MOD_ID`.
5. Note the game domain in the URL (`slaythespire2`). That's `NEXUS_GAME_DOMAIN`.

## 3. Generate a Nexus personal API key

1. Go to `https://www.nexusmods.com/users/myaccount?tab=api+access`.
2. Under "Personal API keys" generate one (or use existing). Copy.

## 4. Set the Nexus secrets

```powershell
gh secret set NEXUS_API_KEY     --body "<paste-key>"
gh secret set NEXUS_GAME_DOMAIN --body "slaythespire2"
gh secret set NEXUS_MOD_ID      --body "<numeric id from step 2.4>"
```

## 5. Cut a release

```bash
git tag -a v0.1.1 -m "v0.1.1: <changelog>"
git push origin v0.1.1
```

The `release.yml` workflow fires on tags matching `v*`. It builds, packages, creates a GitHub Release with the zip attached, and (if all six secrets are set) pushes the file to Nexus. The Nexus step is guarded — if `NEXUS_API_KEY` or `NEXUS_MOD_ID` are unset, the release still happens but the Nexus push is skipped, so you can iterate releases on GitHub-only while figuring out the Nexus side.
