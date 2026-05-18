# STS2 mod loader — API conventions (discovered v0.103.2)

Findings from reflecting on `sts2.dll` (the game) and two reference mods (`STS2-RitsuLib`, `BaseLib`).

## Target framework

**`net9.0`** — not net8.0. Both reference mods target `.NETCoreApp,Version=v9.0`. Reference assemblies bundled with the game (GodotSharp, sts2) target net8.0 / net9.0 respectively, so net9.0 mods are forward-compatible.

## Required references

Copy from `<Steam>/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/` into `src/lib/`:

| DLL | Purpose |
|-----|---------|
| `sts2.dll` | The game's own assembly. Source of `MegaCrit.Sts2.Core.Modding.*`, `MegaCrit.Sts2.Core.Nodes.*`. |
| `GodotSharp.dll` | Godot 4 C# bindings (v4.5.1 in this build). |
| `0Harmony.dll` | HarmonyLib v2.4.2. Used for runtime method patching. |

Additional transitive references (only needed if their types are touched): `SmartFormat.dll`, `JetBrains.Annotations.dll`, `Sentry.dll`, `Steamworks.NET.dll`, `MonoMod.*`.

All references must use `<Private>false</Private>` so we don't duplicate them in the published mod folder.

## Entry-point convention

**Attribute-driven.** No base class, no auto-loaded singleton.

```csharp
[MegaCrit.Sts2.Core.Modding.ModInitializer(nameof(Initialize))]
public static class Mod
{
    public static void Initialize()
    {
        // entry point — called once by ModManager.CallModInitializer after the mod's DLL loads
    }
}
```

The `ModInitializerAttribute(string initializerMethod)` constructor takes the name of the static method to call. `ModManager.CallModInitializer(Type)` reflectively invokes the named static method on the type. Both reference mods use `Initialize` as the method name; we follow suit.

The mod is **not** a `Node` subclass. To hook scene events we obtain the `SceneTree` ourselves at entry time:

```csharp
var tree = (SceneTree)Godot.Engine.GetMainLoop();
tree.NodeAdded += node => { /* ... */ };
tree.Root.SizeChanged += () => { /* ... */ };
```

## Manifest schema (authoritative)

From `MegaCrit.Sts2.Core.Modding.ModManifest`:

```csharp
public sealed class ModManifest
{
    public string id;
    public string name;
    public string author;
    public string description;
    public string version;
    public bool   hasPck;
    public bool   hasDll;
    public List<string> dependencies;   // mod ids this mod depends on
    public bool   affectsGameplay;
}
```

The JSON keys match the field names exactly (case-sensitive). Extra keys the game ignores but that the community uses:
- `pck_name` — shipped mods use this; appears community-conventional, not parsed by the manifest model above. We ship it for compatibility with tools that read it.
- `min_game_version` — same, community-conventional.
- A sibling `compat-target.txt` file pinning the exact tested game version (e.g. `0.103.2`).

## ModManager surface

`MegaCrit.Sts2.Core.Modding.ModManager` is the global static manager. Useful methods/properties:
- `static IReadOnlyList<Mod> Mods` — every loaded mod.
- `static event Action<Mod> OnModDetected` — fires per mod as the loader discovers them.
- `static bool IsRunningModded()` — true when any mod is active.
- `static bool HasHarmonyPatches()` — true when any mod has applied Harmony patches.

## Patching with Harmony

The game ships HarmonyLib 2.4.2 (`0Harmony.dll`). Standard usage:

```csharp
var harmony = new HarmonyLib.Harmony("sts2-ultrawide");
harmony.PatchAll(typeof(Mod).Assembly);   // or apply specific patches manually
```

Harmony is the right tool when re-anchoring a HUD element or unlocking the resolution dropdown requires intercepting a game method rather than just observing scene events.

## Logging

`Godot.GD.Print` and `Godot.GD.PrintRich` both work. RitsuLib also exposes a `Logger` abstraction (`STS2RitsuLib.RitsuLibFramework.CreateLogger(modId, logType)`) but we depend on RitsuLib's existence which we explicitly don't. Stick to `GD.Print`/`GD.PrintErr` for now.

## Implications for the design

- Update `SlayTheSpire2Ultrawide.csproj` to `<TargetFramework>net9.0</TargetFramework>` (the plan said net8.0).
- Replace the `partial class Mod : Node` pattern in the plan's Task 10 with the `[ModInitializer]` static class pattern.
- Add `<Reference Include="sts2">` and `<Reference Include="0Harmony">` alongside `GodotSharp` in the csproj.
- The CI workflow needs all three DLLs vendored (`vendor/sts2.dll`, `vendor/0Harmony.dll`, `vendor/GodotSharp.dll`) so build runners can resolve references without a game install.
