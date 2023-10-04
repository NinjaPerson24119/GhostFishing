using Godot;

public partial class InventoryUI : Control {
    private Inventory _inventory;
    private GridContainer _gridContainer = null!;
    private TextureRect _tile = null!;
    private Color _color = new Color(0.72f, 0.44f, 0.10f, 0.5f);
    private ShaderMaterial _material = null!;
    private int _margin = 2;

    public InventoryUI(Inventory inventory) {
        _inventory = inventory;
    }

    public override void _Ready() {
        // define a tile we can duplicate
        _material = new ShaderMaterial() {
            Shader = GD.Load<Shader>("res://common/inventory/ui/InventoryTile.gdshader")
        };
        _tile = new TextureRect() {
            Texture = GD.Load<Texture2D>("res://common/inventory/ui/InventoryTile.png"),
            Material = _material,
        };

        // create a grid container to hold the tiles
        _gridContainer = new GridContainer() {
            Columns = _inventory.Width,
        };
        _gridContainer.AddThemeConstantOverride("h_separation", 0);
        _gridContainer.AddThemeConstantOverride("v_separation", 0);

        // grid needs margin so it doesn't hug its container
        

        // place it in the center of 
        AddChild(_gridContainer);
    }
}
