using Godot;

internal partial class ScreenPadding : Control {
    [Export]
    public int PaddingPx = 16;

    public override void _Ready() {
        OffsetTop = PaddingPx;
        OffsetLeft = PaddingPx;
        OffsetRight = -PaddingPx;
        OffsetBottom = -PaddingPx;
    }
}
