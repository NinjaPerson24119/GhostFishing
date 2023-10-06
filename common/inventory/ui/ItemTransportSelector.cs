using Godot;

public partial class InventoryItemTransportSelector : Node2D {
    private int _tileSize;
    // for scaling simplicity center all the children
    private Sprite2D _outline = new Sprite2D() {
        Name = "Outline",
        Centered = false
    };
    private Vector2 _outlineDefaultScale;
    private Sprite2D _sprite = new Sprite2D() {
        Name = "ItemSprite",
        Centered = true
    };
    private InventoryItemInstance? _item;

    public InventoryItemTransportSelector(int tileSize) {
        _tileSize = tileSize;

        AddChild(_sprite);

        Texture2D outlineTexture = GD.Load<Texture2D>("res://artwork/generated/ui/Selector.png");
        _outline.Texture = outlineTexture;
        _outlineDefaultScale = new Vector2(tileSize / outlineTexture.GetWidth(), tileSize / outlineTexture.GetHeight());
        _outline.Scale = _outlineDefaultScale;
        AddChild(_outline);
    }

    public void UnassignItem() {
        if (_item == null) {
            return;
        }
        _outline.Scale = _outlineDefaultScale;
        _sprite.Visible = false;
    }

    public void AssignItem(InventoryItemInstance item) {
        _item = item;
        OnItemUpdated();
    }

    public void OnItemUpdated() {
        if (_item == null) {
            return;
        }

        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(_item.ItemDefinitionID);
        float width = _tileSize * itemDef.Space.Width;
        float height = _tileSize * itemDef.Space.Height;
        string imagePath = AssetManager.Ref().GetInventoryItemDefinition(_item.ItemDefinitionID).ImagePath;
        Texture2D texture = GD.Load<Texture2D>(imagePath);
        _sprite.Texture = texture;
        _sprite.Position = new Vector2(width / 2, height / 2);
        _sprite.Scale = new Vector2(width / texture.GetWidth(), height / texture.GetHeight());
        _sprite.Rotation = _item.RotationRadians;
        _sprite.Visible = true;

        _outline.Scale = new Vector2(itemDef.Space.Width * _tileSize / _tileSize, itemDef.Space.Height * _tileSize / _tileSize);
    }
}
