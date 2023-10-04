using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control {
    public partial class InventoryTile : Control {
        public Color TileColor = Colors.Green;
        public bool Filled {
            get {
                return _filled;
            }
            set {
                _filled = value;
                UpdateShader();
            }
        }
        private bool _filled;
        public Color BackgroundColor = Colors.Yellow;

        private ShaderMaterial _material = new ShaderMaterial();
        private string _tileImagePath = "res://artwork/generated/ui/InventoryTile.png";
        private string _tileShaderPath = "res://common/inventory/ui/InventoryTile.gdshader";

        public InventoryTile(Color tileColor, Color backgroundColor, bool filled) {
            _material.Shader = GD.Load<Shader>(_tileShaderPath);
            _material.SetShaderParameter("background_color", backgroundColor);
            _material.SetShaderParameter("outer_color", tileColor);

            TileColor = tileColor;
            BackgroundColor = backgroundColor;
            Filled = filled;
            UpdateShader();

            TextureRect tile = new TextureRect() {
                Texture = GD.Load<Texture2D>(_tileImagePath),
                Material = _material,
            };
            AddChild(tile);
        }

        private void UpdateShader() {
            if (_filled) {
                _material.SetShaderParameter("inner_color", TileColor);
            }
            else {
                _material.SetShaderParameter("inner_color", BackgroundColor);
            }
        }
    }

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
    public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    [Export]
    public Color DefaultTileColor = new Color(0.72f, 0.44f, 0.10f, 0.5f);
    [Export]
    public int TileSizePx = 64;

    private string _containerFrameImagePath = "res://artwork/generated/ui/InventoryFrame.png";

    // need to position items relative to grid
    private GridContainer _gridContainer = new GridContainer();
    // need to change tile colors
    private List<Control> _tileControls = new List<Control>();

    private Inventory _inventory = null!;

    public override void _Ready() {
        // get inventory from player
        PlayerStateView player = AssetManager.Ref().GetPlayerView(0);
        _inventory = player.BoatInventory;

        _gridContainer.Columns = _inventory.Width;
        _gridContainer.CustomMinimumSize = new Vector2(_inventory.Width * TileSizePx, _inventory.Height * TileSizePx);
        _gridContainer.AddThemeConstantOverride("h_separation", 0);
        _gridContainer.AddThemeConstantOverride("v_separation", 0);

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

        // add tiles to grid
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                Control control;
                if (_inventory.SpaceUsable(x, y)) {
                    bool spaceFilled = _inventory.SpaceFilled(x, y);
                    Color color = DefaultTileColor;
                    // get the item's background color if it has one
                    if (spaceFilled) {
                        InventoryItemInstance? item = _inventory.ItemAt(x, y);
                        if (item == null) {
                            throw new System.Exception("InventoryUI: item is null but space is filled");
                        }
                        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);
                        color = itemDef.BackgroundColorOverride ?? DefaultTileColor;
                    }
                    InventoryTile tile = new InventoryTile(color, BackgroundColor, spaceFilled);
                    control = tile;
                }
                else {
                    // use empty Control as spacer
                    control = new Control();
                }
                control.Size = new Vector2(TileSizePx, TileSizePx);
                control.CustomMinimumSize = new Vector2(TileSizePx, TileSizePx);
                _gridContainer.AddChild(control);
                _tileControls.Add(control);
            }
        }

        center.AddChild(_gridContainer);
        containerFrameImage.AddChild(center);
        if (backgroundImage != null) {
            containerBackgroundColor.AddChild(backgroundImage);
        }
        containerBackgroundColor.AddChild(containerFrameImage);
        AddChild(containerBackgroundColor);
    }
}
