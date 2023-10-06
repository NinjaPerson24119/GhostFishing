using Godot;

public partial class InventoryItemTransportSelector : Node2D {
    private int _tileSize;
    private ColorRect _highlight = new ColorRect() {
        Name = "Highlight",
        Color = new Color(1.0f, 1.0f, 1.0f, 0.5f)
    };
    private Sprite2D _sprite = new Sprite2D() {
        Name = "ItemSprite",
        Centered = true
    };
    private InventoryItemInstance? _item;

    public InventoryItemTransportSelector(int tileSize) {
        _tileSize = tileSize;
        Scale = new Vector2(_tileSize, _tileSize);

        _highlight.Size = new Vector2(1, 1);
        AddChild(_highlight);

        AddChild(_sprite);
    }

    public void UnassignItem() {
        if (_item == null) {
            return;
        }
        Scale = new Vector2(_tileSize, _tileSize);
        _sprite.Visible = false;
    }

    public void AssignItem(InventoryItemInstance item) {
        _item = item;

        string imagePath = AssetManager.Ref().GetInventoryItemDefinition(_item.ItemDefinitionID).ImagePath;
        Texture2D texture = GD.Load<Texture2D>(imagePath);
        _sprite.Texture = texture;
        _sprite.Position = new Vector2(texture.GetWidth() / 2, texture.GetHeight() / 2);
        _sprite.Rotation = _item.RotationRadians;
        _sprite.Visible = true;
        
        Scale = new Vector2(_tileSize / texture.GetWidth(), _tileSize / texture.GetHeight());
    }
}
