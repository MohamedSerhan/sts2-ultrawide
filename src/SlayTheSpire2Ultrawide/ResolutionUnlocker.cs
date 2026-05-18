using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class ResolutionUnlocker
{
    private readonly Config _config;
    private readonly HashSet<ulong> _injected = new();

    public ResolutionUnlocker(Config config) { _config = config; }

    public void OnNodeAdded(Node node)
    {
        if (node is not OptionButton ob) return;
        if (_injected.Contains(ob.GetInstanceId())) return;
        if (!LooksLikeResolutionDropdown(ob)) return;

        var screenSize = DisplayServer.ScreenGetSize();
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
        }
        _injected.Add(ob.GetInstanceId());
        Mod.Log($"injected ultrawide entries into {ob.GetPath()}");
    }

    private static bool LooksLikeResolutionDropdown(OptionButton ob)
    {
        var path = ob.GetPath().ToString().ToLowerInvariant();
        if (path.Contains("resolution")) return true;
        return ob.Name.ToString().Contains("resolution", StringComparison.OrdinalIgnoreCase);
    }
}
