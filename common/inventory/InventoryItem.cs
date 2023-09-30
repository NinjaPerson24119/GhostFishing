public enum InventoryItemRotation {
    None = 0,
    Clockwise90 = 1,
    Clockwise180 = 2,
    Clockwise270 = 3,
}

public class InventoryItemSpatial {
    public int Y { get; set; }
    public int X { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool[] FilledMask { get; set; }
    public InventoryItemRotation Rotation {
        get {
            return _rotation;
        }
        set {
            int diff = (int)value - (int)_rotation;
            if (diff < 0) {
                diff += 4;
            }
            diff = diff % 4;
            for (int i = 0; i < diff; i++) {
                RotateClockwise90();
            }
        }
    }
    InventoryItemRotation _rotation = InventoryItemRotation.None;
    public void RotateClockwise90() {
        Matrix<bool> filledMaskMatrix = new Matrix<bool>() {
            Width = Width,
            Height = Height,
            Data = FilledMask,
        };
        filledMaskMatrix.RotateClockwise90();
        FilledMask = filledMaskMatrix.Data;
    }
}

public class InventoryItem {
    public string UUID { get; set; }
    public InventoryItemSpatial Spatial { get; set; }
}

