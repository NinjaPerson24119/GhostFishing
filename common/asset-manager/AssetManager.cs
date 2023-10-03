using Godot;
using System;
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

    private const string _boatInventoryPath = "res://data/boat-inventory.json";
    private const string _fishDefinitionsPath = "res://data/fish.json";
    private const string _itemCategoryDefinitionsPath = "res://data/item-categories.json";
    private const string _questDefinitionsPath = "res://data/quests.json";

    public const string TemporaryInventoryID = "INVENTORY-dc106fde-f9a0-4a99-8b58-0dee17fce491";
    public const string PlayerOneBoatInventoryID = "INVENTORY-31cdec79-2a3b-4f4d-9e23-a878915f3973";
    public const string PlayerOneQuestInventoryID = "INVENTORY-96c151e3-2436-406c-967c-79a1cc89c3ac";
    public const string PlayerTwoBoatInventoryID = "INVENTORY-e158299f-a54e-42b0-964a-3ac732ec3631";
    public const string PlayerTwoQuestInventoryID = "INVENTORY-f7091de2-6804-4f22-b8f1-d17be782adf6";
    public const string StorageInventoryID = "INVENTORY-9f5d56ed-c8aa-4899-b749-66bd734141fb";

    public Dictionary<string, Inventory> Inventories = new Dictionary<string, Inventory>();
    public Dictionary<string, InventoryItemDefinition> InventoryItemDefinitions = new Dictionary<string, InventoryItemDefinition>();
    public Dictionary<string, InventoryItemCategory> InventoryItemCategories = new Dictionary<string, InventoryItemCategory>();
    public Dictionary<string, QuestDefinition> QuestDefinitions = new Dictionary<string, QuestDefinition>();

    public AssetManager() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
        LoadAssets();
    }

    private T? LoadAssetFromJSON<T>(string filePath) {
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
        catch (Exception e) {
            GD.PrintErr($"Error loading asset from JSON: {filePath}. Error: {e}");
            return default;
        }
    }

    private void LoadAssets() {
        // load initial boat inventory shape
        LoadBoatInventories();

        // TODO: instantiate temp inventory to size large enough for any pickup

        LoadItemCategories();
        LoadQuestDefinitions();
        LoadFishDefinitions();
    }

    public void LoadBoatInventories() {
        InventoryDTO? boatInventory = LoadAssetFromJSON<InventoryDTO>(_boatInventoryPath);
        if (boatInventory != null) {
            try {
                Inventory model = new Inventory(boatInventory);
                Inventories.Add(PlayerOneBoatInventoryID, model);
                Inventories.Add(PlayerTwoBoatInventoryID, model);
            }
            catch (Exception e) {
                GD.PrintErr($"Error loading boat inventory from DTO: {e}");
            }
        }
    }

    public void LoadItemCategories() {
        Dictionary<string, InventoryItemCategoryDTO>? itemCategories = LoadAssetFromJSON<Dictionary<string, InventoryItemCategoryDTO>>(_itemCategoryDefinitionsPath);
        if (itemCategories != null) {
            try {
                foreach (var kv in itemCategories) {
                    InventoryItemCategory model = new InventoryItemCategory(kv.Value);
                    InventoryItemCategories.Add(kv.Key, model);
                }
            }
            catch (Exception e) {
                GD.PrintErr($"Error loading item categories from DTO: {e}");
            }
        }
    }

    public void LoadQuestDefinitions() {
        Dictionary<string, QuestDefinitionDTO>? questDefinitions = LoadAssetFromJSON<Dictionary<string, QuestDefinitionDTO>>(_questDefinitionsPath);
        if (questDefinitions != null) {
            try {
                foreach (var kv in questDefinitions) {
                    QuestDefinition model = new QuestDefinition(kv.Value);
                    QuestDefinitions.Add(kv.Key, model);
                }
            }
            catch (Exception e) {
                GD.PrintErr($"Error loading quest definitions from DTO: {e}");
            }
        }
    }

    public void LoadFishDefinitions() {
        Dictionary<string, FishDefinitionDTO>? fishDefinitions = LoadAssetFromJSON<Dictionary<string, FishDefinitionDTO>>(_fishDefinitionsPath);
        if (fishDefinitions != null) {
            try {
                foreach (var kv in fishDefinitions) {
                    InventoryItemDefinition model = new InventoryItemDefinition(kv.Value);
                    InventoryItemDefinitions.Add(kv.Key, model);
                }
            }
            catch (Exception e) {
                GD.PrintErr($"Error loading fish definitions from DTO: {e}");
            }
        }
    }

    // TODO: validate referential integrity
    // public void ValidateAssetReferrers() {}

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
