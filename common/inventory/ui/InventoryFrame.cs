using Godot;

public partial class InventoryFrame : Control {
    [Export]
    public int ContainerWidthPx = 800;
    [Export]
    public int ContainerHeightPx = 800;
    // if this is set then the container dimensions are ignored
    [Export]
    public bool FitContainerToGrid = true;
    [Export]
    public int GridMarginPx = 40;
    [Export]
    public Color DefaultTileColor = new Color(0.72f, 0.44f, 0.10f, 0.5f);
    [Export]
    public int TileSizePx = 64;
    [Export]
    public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

    private InventoryGrid _inventoryGrid = null!;
    private string _containerFrameImagePath = "res://artwork/generated/ui/InventoryFrame.png";

    public override void _Ready() {
        // get inventory from player
        PlayerStateView player = AssetManager.Ref().GetPlayerView(0);
        Inventory _inventory = player.BoatInventory;

        // create grid
        _inventoryGrid = new InventoryGrid(
            inventory: _inventory,
            tileSizePx: TileSizePx,
            defaultTileColor: DefaultTileColor,
            backgroundColor: BackgroundColor
        );

        // fit container dimensions
        if (FitContainerToGrid) {
            ContainerWidthPx = _inventory.Width * TileSizePx + GridMarginPx * 2;
            ContainerHeightPx = _inventory.Height * TileSizePx + GridMarginPx * 2;
        }

        // add a background image
        TextureRect? backgroundImage = null;
        if (_inventory.BackgroundImagePath != null) {
            backgroundImage = new TextureRect() {
                Texture = GD.Load<Texture2D>(_inventory.BackgroundImagePath),
                Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
            };
        }

        // add a container color
        ColorRect containerBackgroundColor = new ColorRect() {
            Color = BackgroundColor,
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        // add a container frame image
        TextureRect containerFrameImage = new TextureRect() {
            Texture = GD.Load<Texture2D>(_containerFrameImagePath),
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        // center grid in container
        CenterContainer center = new CenterContainer() {
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        center.AddChild(_inventoryGrid);
        containerFrameImage.AddChild(center);
        if (backgroundImage != null) {
            containerBackgroundColor.AddChild(backgroundImage);
        }
        containerBackgroundColor.AddChild(containerFrameImage);
        AddChild(containerBackgroundColor);
    }
}