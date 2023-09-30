public class Inventory {
    public int Width { get; set; }
    public int Height { get; set; }
    // slots that cannot ever be used (e.g. if the inventory isn't a perfect rectangle)
    public bool[] UsableMask { get; set; }
    public InventoryItem[] Items { get; set; }

    // slots that are currently occupied
    private bool[] filledMask;

    public Inventory(int width, int height, bool[] usableMask, InventoryItem[] items = null) {
        Width = width;
        Height = height;
        UsableMask = usableMask;
        if (items != null) {
            foreach (InventoryItem item in items) {
                if (item.Spatial == null) {
                    continue;
                }
                PlaceItem(item, item.Spatial.X, item.Spatial.Y);
            }
        }
    }

    public bool CanPlaceItem(InventoryItem item, int x, int y) {
        if (item.Spatial == null) {
            return true;
        }
        if (x < 0 || y < 0 || x + item.Spatial.Width > Width || y + item.Spatial.Height > Height) {
            return false;
        }
        for (int i = 0; i < item.Spatial.Width; i++) {
            for (int j = 0; j < item.Spatial.Height; j++) {
                int indexConsidered = (y + j) * Width + (x + i);
                if (!UsableMask[indexConsidered]) {
                    return false;
                }
                if (filledMask[indexConsidered]) {
                    return false;
                }
            }
        }
        return true;
    }

    public bool[] GenerateFilledMask() {
        bool[] filledMask = new bool[Width * Height];
        foreach (InventoryItem item in Items) {
            for (int i = 0; i < item.Spatial.Width; i++) {
                for (int j = 0; j < item.Spatial.Height; j++) {
                    int indexConsidered = (item.Spatial.Y + j) * Width + (item.Spatial.X + i);
                    DebugTools.Assert(UsableMask[indexConsidered]);
                    DebugTools.Assert(!filledMask[indexConsidered]);
                    filledMask[indexConsidered] = true;
                }
            }
        }
        return filledMask;
    }

    public bool PlaceItem(InventoryItem item, int x, int y) {
        DebugTools.Assert(item.Spatial == null, $"Cannot place item {item.UUID} without spatial data");
        if (!CanPlaceItem(item, x, y)) {
            return false;
        }
        item.Spatial.X = x;
        item.Spatial.Y = y;
        filledMask = GenerateFilledMask();
        return true;
    }
}
