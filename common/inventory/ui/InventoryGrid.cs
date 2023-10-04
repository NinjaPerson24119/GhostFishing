using Godot;
using System.Collections.Generic;

public partial class InventoryGrid : GridContainer {
    public partial class InventoryTile : TextureRect {
        private static Color _HoverOuterColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        private static Color _HoverInnerColor = new Color(0.8f, 0.8f, 0.8f, 1.8f);
        public Color TileColor = Colors.Green;
        public bool Hovered {
            get {
                return _hovered;
            }
            set {
                _hovered = value;
                UpdateShader();
            }
        }
        private bool _hovered;
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
            FocusMode = FocusModeEnum.All;

            _material.Shader = GD.Load<Shader>(_tileShaderPath);
            _material.SetShaderParameter("background_color", backgroundColor);
            _material.SetShaderParameter("outer_color", tileColor);

            TileColor = tileColor;
            BackgroundColor = backgroundColor;
            Filled = filled;
            UpdateShader();

            Texture = GD.Load<Texture2D>(_tileImagePath);
            Material = _material;
        }

        public override void _Notification(int what) {
            switch (what) {
                case (int)NotificationFocusEnter:
                    Hovered = true;
                    break;

                case (int)NotificationFocusExit:
                    Hovered = false;
                    break;
            }
        }

        private void UpdateShader() {
            if (Hovered) {
                _material.SetShaderParameter("outer_color", _HoverOuterColor);
                _material.SetShaderParameter("inner_color", _HoverInnerColor);
                return;
            }
            else {
                _material.SetShaderParameter("outer_color", TileColor);
            }

            if (Filled) {
                _material.SetShaderParameter("inner_color", TileColor);
            }
            else {
                _material.SetShaderParameter("inner_color", BackgroundColor);
            }
        }
    }

    private Inventory _inventory;
    private int _tileSizePx = 64;
    private Color _defaultTileColor;
    private Color _backgroundColor;

    // need to change tile colors
    private List<Control> _tileControls = new List<Control>();

    public InventoryGrid(Inventory inventory, int tileSizePx, Color defaultTileColor, Color backgroundColor) {
        _inventory = inventory;
        _tileSizePx = tileSizePx;
        _defaultTileColor = defaultTileColor;
        _backgroundColor = backgroundColor;
    }

    public override void _Ready() {
        Columns = _inventory.Width;
        CustomMinimumSize = new Vector2(_inventory.Width * _tileSizePx, _inventory.Height * _tileSizePx);
        AddThemeConstantOverride("h_separation", 0);
        AddThemeConstantOverride("v_separation", 0);

        // add tiles to grid
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                Control control;
                if (_inventory.SpaceUsable(x, y)) {
                    bool spaceFilled = _inventory.SpaceFilled(x, y);
                    Color color = _defaultTileColor;
                    // get the item's background color if it has one
                    if (spaceFilled) {
                        InventoryItemInstance? item = _inventory.ItemAt(x, y);
                        if (item == null) {
                            throw new System.Exception("InventoryUI: item is null but space is filled");
                        }
                        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);
                        color = itemDef.BackgroundColorOverride ?? _defaultTileColor;
                    }
                    InventoryTile tile = new InventoryTile(color, _backgroundColor, spaceFilled);
                    control = tile;
                }
                else {
                    // use empty Control as spacer
                    control = new Control();
                }
                control.Size = new Vector2(_tileSizePx, _tileSizePx);
                control.CustomMinimumSize = new Vector2(_tileSizePx, _tileSizePx);
                AddChild(control);
                _tileControls.Add(control);
            }
        }
    }
}
