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
        Assert.Equal(24, c.HudEdgePaddingPx);
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
