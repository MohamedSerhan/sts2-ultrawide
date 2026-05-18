using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class ResolutionUnlocker
{
    private readonly Config _config;
    private readonly HashSet<ulong> _injected = new();

    public ResolutionUnlocker(Config config) { _config = config; }

    /// <summary>Walk the existing scene tree once — for dropdowns created before our subscription.</summary>
    public void ScanExistingTree(Node root)
    {
        TryInject(root);
        foreach (var child in root.GetChildren()) ScanExistingTree(child);
    }

    public void OnNodeAdded(Node node) => TryInject(node);

    private void TryInject(Node node)
    {
        if (node is not OptionButton ob) return;
        if (_injected.Contains(ob.GetInstanceId())) return;
        if (!LooksLikeResolutionDropdown(ob)) return;

        var screenSize = DisplayServer.ScreenGetSize();
        var added = 0;
        foreach (var (w, h) in AspectMath.ResolutionLadder())
        {
            if (w > screenSize.X || h > screenSize.Y) continue;
            var label = $"{w} x {h}";
            var existing = false;
            for (int i = 0; i < ob.ItemCount; i++)
            {
                if (ob.GetItemText(i) == label) { existing = true; break; }
            }
            if (existing) continue;
            ob.AddItem(label);
            ob.SetItemMetadata(ob.ItemCount - 1, new Vector2I(w, h));
            added++;
        }
        _injected.Add(ob.GetInstanceId());
        Mod.Log($"injected {added} ultrawide entries into {ob.GetPath()} (screen {screenSize.X}x{screenSize.Y}, {ob.ItemCount} items total)");
    }

    /// <summary>
    /// Heuristic: the resolution dropdown is the OptionButton whose entries look like resolutions
    /// (one of them is the canonical "1920 x 1080" fingerprint), or whose path/name contains "resolution".
    /// </summary>
    private static bool LooksLikeResolutionDropdown(OptionButton ob)
    {
        var path = ob.GetPath().ToString().ToLowerInvariant();
        var name = ob.Name.ToString().ToLowerInvariant();
        if (path.Contains("resolution") || name.Contains("resolution") || name.Contains("windowed")) return true;

        // Content fingerprint — vanilla list always contains 1920 x 1080.
        for (int i = 0; i < ob.ItemCount; i++)
        {
            var text = ob.GetItemText(i);
            if (text == "1920 x 1080" || text == "1920x1080") return true;
        }
        return false;
    }
}
