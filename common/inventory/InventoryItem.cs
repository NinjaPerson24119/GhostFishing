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
    public InventoryItemRotation Rotation { get; set; }
    // rotation by 90 degrees is equivalent to transposing and reversing column order
    public InventoryItemSpatial Rotated(InventoryItemRotation rotation) {
        InventoryItemSpatial newSpatial = new InventoryItemSpatial() {
            Y = Y,
            X = X,
            Width = Width,
            Height = Height,
            FilledMask = new bool[Width * Height],
            Rotation = rotation,
        };

        // not necessarily square, so swap dims on uneven rotation
        int noRotations = (int)rotation;
        if (noRotations % 2 != 0) {
            newSpatial.Width = Height;
            newSpatial.Height = Width;
        }
        return newSpatial;
    }
    private RotateClockwise90(bool[] matrix) {
        // transpose
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                newSpatial.FilledMask[x * Height + y] = FilledMask[y * Width + x];
            }
        }

        // reverse columns
        for (int y = 0; y < newSpatial.Height; y++) {
            for (int x = 0; x < newSpatial.Width / 2; x++) {
                int leftIdx = y * newSpatial.Width + x;
                int rightIdx = y * newSpatial.Width + newSpatial.Width - 1 - x;
                bool temp = newSpatial.FilledMask[leftIdx];
                newSpatial.FilledMask[leftIdx] = newSpatial.FilledMask[rightIdx];
                newSpatial.FilledMask[rightIdx] = temp;
            }
        }
    }
}

public class InventoryItem {
    public string UUID { get; set; }
    public InventoryItemSpatial Spatial { get; set; }

    // TODO: support rotation
    // - Store rotation
    // - Get rotated spatial
}

