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
