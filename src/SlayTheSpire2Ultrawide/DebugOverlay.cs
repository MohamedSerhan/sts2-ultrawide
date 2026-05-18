using Godot;

namespace SlayTheSpire2Ultrawide;

internal sealed class DebugOverlay
{
    private CanvasLayer? _layer;
    private Label? _label;

    public void Refresh(Node root)
    {
        if (_layer is null || !GodotObject.IsInstanceValid(_layer))
        {
            _layer = new CanvasLayer { Layer = 128 };
            _label = new Label
            {
                Text = "sts2-ultrawide debug",
                Modulate = new Color(0, 1, 1, 0.9f),
                Position = new Vector2(8, 8),
            };
            _layer.AddChild(_label);
            root.AddChild(_layer);
        }
        var vp = _layer.GetViewport().GetVisibleRect().Size;
        _label!.Text = $"sts2-ultrawide  viewport {vp.X:0}x{vp.Y:0}  aspect {(vp.X / vp.Y):0.000}";
    }
}
