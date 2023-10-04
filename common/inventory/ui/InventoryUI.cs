using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control {
    [Export]
    public int ContainerWidthPx = 800;
    [Export]
    public int ContainerHeightPx = 800;
    // if this is set then the container dimensions are ignored
    [Export]
    public bool FitContainerToGrid = true;
    [Export]
    public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    [Export]
    public int GridMarginPx = 40;
    [Export]
    public Color TileColor = new Color(0.72f, 0.44f, 0.10f, 0.5f);
    [Export]
    public int TileSizePx = 64;

    private string _containerFrameImagePath = "res://artwork/generated/ui/InventoryFrame.png";
    private string _tileImagePath = "res://artwork/generated/ui/InventoryTile.png";
    private string _tileShaderPath = "res://common/inventory/ui/InventoryTile.gdshader";

    private Inventory _inventory = null!;
    private ShaderMaterial _material;

    private List<Control> _layoutControls = new List<Control>();
    private GridContainer _gridContainer = new GridContainer();
    private List<Control> _gridControls = new List<Control>();

    public InventoryUI() {
        _material = new ShaderMaterial() {
            Shader = GD.Load<Shader>(_tileShaderPath)
        };
        _material.SetShaderParameter("color", TileColor);
    }

    public override void _Ready() {
        // get inventory from player
        PlayerStateView player = AssetManager.Ref().GetPlayerView(0);
        _inventory = player.BoatInventory;

        // define a tile we can duplicate
        TextureRect tile = new TextureRect() {
            Texture = GD.Load<Texture2D>(_tileImagePath),
            Material = _material,
        };

        _gridContainer.Columns = _inventory.Width;
        _gridContainer.Size = new Vector2(_inventory.Width * TileSizePx, _inventory.Height * TileSizePx);
        _gridContainer.AddThemeConstantOverride("h_separation", 0);
        _gridContainer.AddThemeConstantOverride("v_separation", 0);
        // minimum size must be set for empty Controls to act as spacers
        //_gridContainer.CustomMinimumSize = new Vector2(TileSizePx, TileSizePx);

        // fit container dimensions
        if (FitContainerToGrid) {
            ContainerWidthPx = _inventory.Width * TileSizePx + GridMarginPx * 2;
            ContainerHeightPx = _inventory.Height * TileSizePx + GridMarginPx * 2;
        }

        // add a background color
        ColorRect containerBackgroundColor = new ColorRect() {
            Color = BackgroundColor,
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        // add a background image
        TextureRect containerFrameImage = new TextureRect() {
            Texture = GD.Load<Texture2D>(_containerFrameImagePath),
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };
        _layoutControls.Add(containerFrameImage);

        // center grid in container
        CenterContainer center = new CenterContainer() {
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };
        _layoutControls.Add(center);

        // add tiles to grid
        for (int x = 0; x < _inventory.Width; x++) {
            for (int y = 0; y < _inventory.Height; y++) {
                Control control;
                if (_inventory.SpaceUsable(x, y)) {
                    control = (TextureRect)tile.Duplicate();
                }
                else {
                    // use empty Control as spacer
                    control = new Control();
                    control.Size = new Vector2(TileSizePx, TileSizePx);
                }
                _gridControls.Add(control);
                _gridContainer.AddChild(control);
            }
        }

        // structure layout
        center.AddChild(_gridContainer);
        containerFrameImage.AddChild(center);
        containerBackgroundColor.AddChild(containerFrameImage);
        AddChild(containerBackgroundColor);
    }
}
