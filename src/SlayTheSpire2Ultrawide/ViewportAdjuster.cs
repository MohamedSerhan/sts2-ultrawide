using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class ViewportAdjuster
{
    private readonly Config _config;
    private bool _stretchModeApplied;

    public ViewportAdjuster(Config config) { _config = config; }

    public void Apply(int width, int height)
    {
        if (!AspectMath.IsUltrawide(width, height)) return;
        var multiplier = (float)AspectMath.CameraXMultiplier(width, height);

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root is null) return;
        var window = tree.Root;

        if (!_stretchModeApplied)
        {
            window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
            window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            _stretchModeApplied = true;
            Mod.Log($"set content scale aspect=Expand, mode=CanvasItems");
        }

        AdjustCamerasIn(window, multiplier);
    }

    private static void AdjustCamerasIn(Node root, float multiplier)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Camera2D cam2d)
            {
                cam2d.Zoom = new Vector2(cam2d.Zoom.X / multiplier, cam2d.Zoom.Y);
            }
            AdjustCamerasIn(child, multiplier);
        }
    }
}
