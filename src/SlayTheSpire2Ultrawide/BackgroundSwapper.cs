using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class BackgroundSwapper
{
    private readonly Config _config;

    public BackgroundSwapper(Config config) { _config = config; }

    public void Apply(int width, int height, Node? scene)
    {
        if (scene is null) return;
        if (!AspectMath.IsUltrawide(width, height)) return;
        Walk(scene, width, height);
    }

    public void ApplyToSubtree(Node root, int width, int height)
    {
        if (!AspectMath.IsUltrawide(width, height)) return;
        Walk(root, width, height);
    }

    private void Walk(Node node, int width, int height)
    {
        if (node is Sprite2D sprite && IsBackground(sprite))
        {
            TrySwapTexture(sprite, width, height);
        }
        foreach (var child in node.GetChildren()) Walk(child, width, height);
    }

    private static bool IsBackground(Sprite2D sprite)
    {
        var n = sprite.Name.ToString().ToLowerInvariant();
        if (n.Contains("background") || n.Contains("bg")) return true;
        var parent = sprite.GetParent();
        if (parent is null) return false;
        var pn = parent.Name.ToString().ToLowerInvariant();
        return pn.Contains("background") || pn.Contains("bg");
    }

    private void TrySwapTexture(Sprite2D sprite, int width, int height)
    {
        var tree = sprite.GetTree();
        var sceneRoot = tree?.CurrentScene?.Name.ToString().ToLowerInvariant() ?? "unknown";
        var kind = sceneRoot.Contains("combat") ? "combat"
                 : sceneRoot.Contains("map") ? "map"
                 : sceneRoot.Contains("shop") ? "shop"
                 : sceneRoot.Contains("event") ? "event"
                 : sceneRoot.Contains("title") ? "title"
                 : null;
        if (kind is null) return;

        var widest = PickPlate(width);
        if (widest is null) return;

        var path = $"res://assets/backgrounds/{kind}/{widest}.png";
        if (!ResourceLoader.Exists(path)) return;
        var tex = ResourceLoader.Load<Texture2D>(path);
        if (tex is null) return;

        sprite.Texture = tex;
        var scaleX = (float)width / tex.GetWidth();
        sprite.Scale = new Vector2(scaleX, scaleX);
        if (_config.VerboseLog) Mod.Log($"swapped {sprite.GetPath()} -> {kind}/{widest}.png");
    }

    private static string? PickPlate(int width)
    {
        var available = new[] { 2560, 3440, 3840, 5120 };
        var best = available.Where(w => w <= width).OrderByDescending(w => w).FirstOrDefault();
        return best == 0 ? null : best.ToString();
    }
}
