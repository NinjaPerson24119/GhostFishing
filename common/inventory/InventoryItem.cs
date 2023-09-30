public class InventoryItemSpatial {
    public int Y { get; set; }
    public int X { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool[] FilledMask { get; set; }
}

public class InventoryItem {
    public string UUID { get; set; }
    public InventoryItemSpatial Spatial { get; set; }
}

