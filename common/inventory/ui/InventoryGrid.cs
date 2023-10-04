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
                if (value) {
                    EmitSignal(SignalName.Focused, _position);
                }
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
        private Vector2I _position;

        private const string _tileImagePath = "res://artwork/generated/ui/InventoryTile.png";
        private const string _tileShaderPath = "res://common/inventory/ui/InventoryTile.gdshader";

        [Signal]
        public delegate void FocusedEventHandler(Vector2I position);

        public InventoryTile(Vector2I position, Color tileColor, Color backgroundColor, bool filled) {
            FocusMode = FocusModeEnum.All;

            _position = position;

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
    private List<Control> _tileControls = new List<Control>();
    private Vector2I _focusedTilePosition = new Vector2I(-1, -1);

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
                    Vector2I position = new Vector2I(x, y);
                    InventoryTile tile = new InventoryTile(position, color, _backgroundColor, spaceFilled);
                    tile.Focused += OnTileFocused;
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

        // assign tile neighbors
        // the default grid assignments don't work because we have empty controls
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                int index = y * _inventory.Width + x;
                Control control = _tileControls[index];
                if (control is InventoryTile tile) {
                    // top
                    int neighborIdx = (y - 1) * _inventory.Width + x;
                    tile.FocusNeighborTop = GetTileNodePath(neighborIdx);

                    // bottom
                    neighborIdx = (y + 1) * _inventory.Width + x;
                    tile.FocusNeighborBottom = GetTileNodePath(neighborIdx);

                    // left
                    neighborIdx = y * _inventory.Width + (x - 1);
                    tile.FocusNeighborLeft = GetTileNodePath(neighborIdx);

                    // right
                    neighborIdx = y * _inventory.Width + (x + 1);
                    tile.FocusNeighborRight = GetTileNodePath(neighborIdx);
                }
            }
        }
    }

    private NodePath? GetTileNodePath(int idx) {
        if (idx < 0 || idx >= _tileControls.Count) {
            return null;
        }
        Control control = _tileControls[idx];
        if (control is InventoryTile tile) {
            return tile.GetPath();
        }
        return null;
    }

    private void OnTileFocused(Vector2I position) {
        _focusedTilePosition = position;
        GD.Print($"Focused tile at {position}");
    }

    public void FocusFirstTile() {
        int i = 0;
        while (i < _tileControls.Count) {
            Control control = _tileControls[i];
            if (control is InventoryTile tile) {
                tile.GrabFocus();
                return;
            }
            i++;
        }
        if (i == _tileControls.Count) {
            throw new System.Exception("Failed to focus first tile because no tiles were found.");
        }
    }
}
