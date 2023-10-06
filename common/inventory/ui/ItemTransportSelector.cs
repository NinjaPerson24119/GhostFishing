using Godot;

public partial class InventoryItemTransportSelector : Node2D {
    private int _tileSize;
    private Sprite2D _outline = new Sprite2D() {
        Name = "Outline",
    };
    private Sprite2D _sprite = new Sprite2D() {
        Name = "ItemSprite",
        Centered = true
    };
    private InventoryItemInstance? _item;

    public InventoryItemTransportSelector(int tileSize) {
        _tileSize = tileSize;

        Texture2D outlineTexture = GD.Load<Texture2D>("res://artwork/generated/ui/Selector.png");
        _outline.Texture = outlineTexture;
        _outline.Scale = new Vector2(tileSize / outlineTexture.GetWidth(), tileSize / outlineTexture.GetHeight());
        AddChild(_outline);

        AddChild(_sprite);
    }

    public void UnassignItem() {
        if (_item == null) {
            return;
        }
        Scale = new Vector2(1, 1);
        _sprite.Visible = false;
    }

    public void AssignItem(InventoryItemInstance item) {
        _item = item;

        string imagePath = AssetManager.Ref().GetInventoryItemDefinition(_item.ItemDefinitionID).ImagePath;
        Texture2D texture = GD.Load<Texture2D>(imagePath);
        _sprite.Texture = texture;
        _sprite.Position = new Vector2(_tileSize / 2, _tileSize / 2);
        _sprite.Scale = new Vector2(_tileSize / texture.GetWidth(), _tileSize / texture.GetHeight());
        _sprite.Rotation = _item.RotationRadians;
        _sprite.Visible = true;

        // width / height
        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(_item.ItemDefinitionID);
        Scale = new Vector2(itemDef.Space.Width, itemDef.Space.Height);
    }
}
