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

    public Inventory DefaultInventory;

    // TODO: add more asset types
    public Dictionary<string, FishDefinition> FishDefinitions;
    public Dictionary<string, InventoryItemDefinition> InventoryItemDefinitions;

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
