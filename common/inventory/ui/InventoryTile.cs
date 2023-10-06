using Godot;

public partial class InventoryTile : TextureRect {
    private static Color _HoverOuterColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private static Color _HoverInnerColor = new Color(0.8f, 0.8f, 0.8f, 1.8f);
    private Color TileColor = Colors.Green;
    private bool isHovered {
        get {
            return _isHovered;
        }
        set {
            _isHovered = value;
            if (value) {
                EmitSignal(SignalName.Focused, _position);
            }
            UpdateShader();
        }
    }
    private bool _isHovered;
    public bool IsFilled {
        get {
            return _isFilled;
        }
        set {
            _isFilled = value;
            UpdateShader();
        }
    }
    private bool _isFilled;
    private Color BackgroundColor = Colors.Yellow;

    private ShaderMaterial _material = new ShaderMaterial();
    private Vector2I _position;
    private Vector2 _lastGlobalPosition;

    private const string _tileImagePath = "res://artwork/generated/ui/InventoryTile.png";
    private const string _tileShaderPath = "res://common/inventory/ui/InventoryTile.gdshader";

    [Signal]
    public delegate void FocusedEventHandler(Vector2I position);
    [Signal]
    public delegate void GlobalPositionChangedEventHandler(Vector2 globalPosition);

    public InventoryTile(Vector2I position, Color tileColor, Color backgroundColor, bool isFilled) {
        FocusMode = FocusModeEnum.All;

        _position = position;

        _material.Shader = GD.Load<Shader>(_tileShaderPath);
        _material.SetShaderParameter("background_color", backgroundColor);
        _material.SetShaderParameter("outer_color", tileColor);

        TileColor = tileColor;
        BackgroundColor = backgroundColor;
        IsFilled = isFilled;
        UpdateShader();

        Texture = GD.Load<Texture2D>(_tileImagePath);
        Material = _material;
    }

    public override void _Process(double delta) {
        // NOTIFICATION_TRANSFORM_CHANGED doesn't work, so do it ourselves
        if (GlobalPosition != _lastGlobalPosition) {
            EmitSignal(SignalName.GlobalPositionChanged, GlobalPosition);
            _lastGlobalPosition = GlobalPosition;
        }
    }

    public override void _Notification(int what) {
        switch (what) {
            case (int)NotificationFocusEnter:
                isHovered = true;
                break;
            case (int)NotificationFocusExit:
                isHovered = false;
                break;
        }
    }

    private void UpdateShader() {
        if (isHovered) {
            _material.SetShaderParameter("outer_color", _HoverOuterColor);
            _material.SetShaderParameter("inner_color", _HoverInnerColor);
            return;
        }
        else {
            _material.SetShaderParameter("outer_color", TileColor);
        }

        if (IsFilled) {
            _material.SetShaderParameter("inner_color", TileColor);
        }
        else {
            _material.SetShaderParameter("inner_color", BackgroundColor);
        }
    }
}
