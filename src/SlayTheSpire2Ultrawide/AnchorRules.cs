using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlayTheSpire2Ultrawide;

public sealed class AnchorRule
{
    [JsonPropertyName("node_path")] public string NodePath { get; set; } = "";
    [JsonPropertyName("anchor_left")] public float AnchorLeft { get; set; }
    [JsonPropertyName("anchor_right")] public float AnchorRight { get; set; }
    [JsonPropertyName("anchor_top")] public float AnchorTop { get; set; }
    [JsonPropertyName("anchor_bottom")] public float AnchorBottom { get; set; }
    [JsonPropertyName("offset_left_px")] public int OffsetLeftPx { get; set; }
    [JsonPropertyName("offset_right_px")] public int OffsetRightPx { get; set; }
    [JsonPropertyName("offset_top_px")] public int OffsetTopPx { get; set; }
    [JsonPropertyName("offset_bottom_px")] public int OffsetBottomPx { get; set; }
}

internal sealed class AnchorRulesDoc
{
    [JsonPropertyName("rules")] public List<AnchorRule> Rules { get; set; } = new();
}

public static class AnchorRules
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static List<AnchorRule> Parse(string json)
    {
        var doc = JsonSerializer.Deserialize<AnchorRulesDoc>(json, Opts) ?? new AnchorRulesDoc();
        return doc.Rules;
    }

    public static bool Match(string pattern, string actualNodePath)
    {
        if (pattern.StartsWith("*/", StringComparison.Ordinal))
        {
            var suffix = pattern[2..];
            return actualNodePath == "/" + suffix || actualNodePath.EndsWith("/" + suffix, StringComparison.Ordinal);
        }
        return pattern == actualNodePath;
    }
}
