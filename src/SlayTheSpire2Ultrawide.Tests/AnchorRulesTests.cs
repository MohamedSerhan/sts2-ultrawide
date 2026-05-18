namespace SlayTheSpire2Ultrawide.Tests;

public class AnchorRulesTests
{
    private const string SampleJson = """
        {
          "rules": [
            {
              "node_path": "*/HUD/TopBar",
              "anchor_left": 0.0,
              "anchor_right": 1.0,
              "anchor_top": 0.0,
              "anchor_bottom": 0.08,
              "offset_left_px": 24,
              "offset_right_px": -24
            },
            {
              "node_path": "*/HUD/EndTurnButton",
              "anchor_left": 1.0,
              "anchor_right": 1.0,
              "anchor_top": 0.85,
              "anchor_bottom": 1.0,
              "offset_left_px": -200,
              "offset_right_px": -24
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ReadsAllRules()
    {
        var rules = AnchorRules.Parse(SampleJson);
        Assert.Equal(2, rules.Count);
        Assert.Equal("*/HUD/TopBar", rules[0].NodePath);
        Assert.Equal(1.0f, rules[0].AnchorRight);
        Assert.Equal(-200, rules[1].OffsetLeftPx);
    }

    [Fact]
    public void Match_GlobMatchesNestedPath()
    {
        var r = AnchorRules.Parse(SampleJson)[0];
        Assert.True(AnchorRules.Match(r.NodePath, "/root/Combat/HUD/TopBar"));
        Assert.True(AnchorRules.Match(r.NodePath, "/root/Map/HUD/TopBar"));
        Assert.False(AnchorRules.Match(r.NodePath, "/root/Combat/HUD/Other"));
    }

    [Fact]
    public void Parse_EmptyJson_ReturnsEmptyList()
    {
        var rules = AnchorRules.Parse("""{"rules": []}""");
        Assert.Empty(rules);
    }
}
