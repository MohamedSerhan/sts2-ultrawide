using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace SlayTheSpire2Ultrawide;

/// <summary>
/// Adds ultrawide entries to the game's resolution whitelist via Harmony.
/// The game's NResolutionDropdown.PopulateDropdownItems() reads from the static
/// GetResolutionWhiteList(); postfix-patching that method makes our entries
/// survive every repopulation (settings reopen, window-mode change, etc.).
/// </summary>
internal sealed class ResolutionUnlocker
{
    private static readonly Harmony _harmony = new(Mod.ModId + ".resolution");

    public ResolutionUnlocker(Config _) { }

    public void ApplyPatches()
    {
        var target = AccessTools.Method(typeof(NResolutionDropdown), "GetResolutionWhiteList");
        if (target is null)
        {
            Mod.Log("could not find NResolutionDropdown.GetResolutionWhiteList - resolution unlock disabled", error: true);
            return;
        }

        var postfix = AccessTools.Method(typeof(ResolutionUnlocker), nameof(WhitelistPostfix));
        _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        Mod.Log("patched NResolutionDropdown.GetResolutionWhiteList");

        // Also patch DoesResolutionFit so the game can't filter ours out via the
        // boundary check. Postfix forces "fits" for any of our ladder entries.
        var fitMethod = AccessTools.Method(typeof(NResolutionDropdown), "DoesResolutionFit");
        if (fitMethod is not null)
        {
            var fitPostfix = AccessTools.Method(typeof(ResolutionUnlocker), nameof(FitPostfix));
            _harmony.Patch(fitMethod, postfix: new HarmonyMethod(fitPostfix));
            Mod.Log("patched NResolutionDropdown.DoesResolutionFit");
        }
    }

    public static void WhitelistPostfix(ref List<Vector2I> __result)
    {
        var seen = new HashSet<Vector2I>(__result);
        foreach (var (w, h) in AspectMath.ResolutionLadder())
        {
            var entry = new Vector2I(w, h);
            if (seen.Add(entry)) __result.Add(entry);
        }
        Mod.Log($"whitelist now has {__result.Count} entries");
    }

    public static void FitPostfix(Vector2I resolution, Vector2I boundaryResolution, ref bool __result)
    {
        if (__result) return; // already accepted
        foreach (var (w, h) in AspectMath.ResolutionLadder())
        {
            if (resolution.X == w && resolution.Y == h)
            {
                __result = true;
                return;
            }
        }
    }
}
