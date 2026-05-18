using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class HudReanchorer
{
    private readonly Config _config;
    private readonly List<AnchorRule> _rules;
    private readonly HashSet<ulong> _applied = new();

    public HudReanchorer(Config config, List<AnchorRule> rules)
    {
        _config = config;
        _rules = rules;
    }

    public void Apply(Node? scene)
    {
        if (scene is null) return;
        Walk(scene);
    }

    public void ApplyToSubtree(Node root) => Walk(root);

    private void Walk(Node node)
    {
        if (node is Control ctl) ApplyMatchingRules(ctl);
        foreach (var child in node.GetChildren()) Walk(child);
    }

    private void ApplyMatchingRules(Control ctl)
    {
        if (_applied.Contains(ctl.GetInstanceId())) return;
        var nodePath = ctl.GetPath().ToString();
        foreach (var rule in _rules)
        {
            if (!AnchorRules.Match(rule.NodePath, nodePath)) continue;

            ctl.AnchorLeft = rule.AnchorLeft;
            ctl.AnchorRight = rule.AnchorRight;
            ctl.AnchorTop = rule.AnchorTop;
            ctl.AnchorBottom = rule.AnchorBottom;
            ctl.OffsetLeft = rule.OffsetLeftPx;
            ctl.OffsetRight = rule.OffsetRightPx;
            ctl.OffsetTop = rule.OffsetTopPx;
            ctl.OffsetBottom = rule.OffsetBottomPx;

            _applied.Add(ctl.GetInstanceId());
            if (_config.VerboseLog) Mod.Log($"reanchored {nodePath} via {rule.NodePath}");
            break;
        }
    }
}
