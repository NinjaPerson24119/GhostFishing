using Godot;

public partial class InventoryTile : Sprite2D {
    public Color TileColor {
        get {
            return _tileColor;
        }
        set {
            _tileColor = value;
            UpdateShader();
        }
    }
    private Color _tileColor;
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

    private Color BackgroundColor;
    private ShaderMaterial _material = new ShaderMaterial();

    private const string _tileImagePath = "res://artwork/generated/ui/InventoryTile.png";
    private const string _tileShaderPath = "res://common/inventory/ui/InventoryTile.gdshader";

    public InventoryTile(Vector2 position, float tileSize, Color tileColor, Color backgroundColor, bool isFilled, bool isVisible) {
        Centered = false;

        _material.Shader = GD.Load<Shader>(_tileShaderPath);
        _material.SetShaderParameter("background_color", backgroundColor);
        _material.SetShaderParameter("outline_color", tileColor);

        TileColor = tileColor;
        BackgroundColor = backgroundColor;
        IsFilled = isFilled;
        UpdateShader();

        Texture = GD.Load<Texture2D>(_tileImagePath);
        Material = _material;

        Visible = isVisible;

        Position = position;
        Scale = new Vector2(tileSize / Texture.GetWidth(), tileSize / Texture.GetHeight());
    }

    private void UpdateShader() {
        _material.SetShaderParameter("outline_color", TileColor);
        if (IsFilled) {
            _material.SetShaderParameter("fill_color", TileColor);
        }
        else {
            _material.SetShaderParameter("fill_color", BackgroundColor);
        }
    }
}
