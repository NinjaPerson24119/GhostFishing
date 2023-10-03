using Godot;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

public interface IGameAssetDTO {
    bool Validate();
    string Stringify();
}

public class AssetDefinitionArray<T> {
    public T[]? Array { get; set; }
}

public partial class AssetManager : Node {
    static SingletonTracker<AssetManager> _singletonTracker = new SingletonTracker<AssetManager>();
    private static AssetManager _singleton { get => _singletonTracker.Ref(); }
    public static AssetManager Ref() {
        return _singleton;
    }

    public const string TemporaryInventoryID = "INVENTORY-dc106fde-f9a0-4a99-8b58-0dee17fce491";
    public const string PlayerOneBoatInventoryID = "INVENTORY-31cdec79-2a3b-4f4d-9e23-a878915f3973";
    public const string PlayerOneQuestInventoryID = "INVENTORY-96c151e3-2436-406c-967c-79a1cc89c3ac";
    public const string PlayerTwoBoatInventoryID = "INVENTORY-e158299f-a54e-42b0-964a-3ac732ec3631";
    public const string PlayerTwoQuestInventoryID = "INVENTORY-f7091de2-6804-4f22-b8f1-d17be782adf6";
    public const string StorageInventoryID = "INVENTORY-9f5d56ed-c8aa-4899-b749-66bd734141fb";

    public Dictionary<string, Inventory>? Inventories;
    public Dictionary<string, InventoryItemDefinition>? InventoryItemDefinitions;
    public Dictionary<string, InventoryItemCategory>? InventoryItemCategories;
    public Dictionary<string, FishDefinition>? FishDefinitions;
    public Dictionary<string, QuestDefinition>? QuestDefinitions;

    public AssetManager() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
        LoadAssets();
    }

    private T LoadAssetFromJSON<T>(string filePath) {
        try {
            string globalizedPath = ProjectSettings.GlobalizePath(filePath);
            string jsonString = File.ReadAllText(globalizedPath);
            return JsonSerializer.Deserialize<T>(jsonString);
        }
        catch (FileNotFoundException) {
            GD.PrintErr("File not found: " + filePath);
            return default;
        }
        catch (JsonException) {
            GD.PrintErr("Error parsing JSON file: " + filePath);
            return default;
        }
    }

    private void LoadAssets() {
        DefaultInventory = LoadAssetFromJSON<Inventory>("res://data/default-inventory.json");
        FishDefinitions = LoadAssetFromJSON<Dictionary<string, FishDefinition>>("res://data/fish-definitions.json");
        foreach (var kv in FishDefinitions) {
            GD.Print($"Fish ({kv.Key}): {kv.Value}");
        }
    }

    public InventoryItemDefinition GetInventoryItemDefinition(string uuid) {
        try {
            return InventoryItemDefinitions[uuid];
        }
        catch (KeyNotFoundException e) {
            GD.PrintErr($"Inventory item definition not found: {uuid}");
            throw e;
        }
    }
}
