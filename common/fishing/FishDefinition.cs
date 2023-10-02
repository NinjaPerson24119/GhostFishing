using Godot;

public class FishDefinition {
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImagePath { get; set; }
    public InventoryItemSpatial InventorySpatial { get; set; }

    public void Load() {

    }

    public override string ToString() {
        string str = $"Name: {Name}, Description: {Description}, ImagePath: {ImagePath}";
        if (InventorySpatial != null) {
            str += $"\nInventorySpatial: {InventorySpatial}";
        }
        return str;
    }
}
