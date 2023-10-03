using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public interface IGameAssetDTO {
    bool IsValid();
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

    private const string _boatInventoryPath = "res://data/default-boat-inventory.json";
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

    private void LoadAssets() {
        LoadDefaultBoatInventories();

        // TODO: instantiate temp inventory to size large enough for any pickup

        LoadAssets(
            InventoryItemDefinitions,
            _fishDefinitionsPath,
            (InventoryItemDefinitionDTO dto) => new InventoryItemDefinition(dto),
            AssetIDValidator.IsInventoryItemDefinitionID
        );
        LoadAssets(
            InventoryItemCategories,
            _itemCategoryDefinitionsPath,
            (InventoryItemCategoryDTO dto) => new InventoryItemCategory(dto),
            AssetIDValidator.IsInventoryItemCategoryID
        );
        LoadAssets(QuestDefinitions,
            _questDefinitionsPath,
            (QuestDefinitionDTO dto) => new QuestDefinition(dto),
            AssetIDValidator.IsQuestID
        );
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

    private delegate T BuildAssetFromDTO<DTO, T>(DTO dto) where DTO : IGameAssetDTO;
    private delegate bool IsIDOfType(string id);
    private void LoadAssets<DTO, T>(Dictionary<string, T> assetDict, string filePath, BuildAssetFromDTO<DTO, T> buildAsset, IsIDOfType isIDOfType) where DTO : IGameAssetDTO {
        Dictionary<string, DTO>? assetDTOs = LoadAssetFromJSON<Dictionary<string, DTO>>(filePath);
        if (assetDTOs != null) {
            foreach (var kv in assetDTOs) {
                T model;
                try {
                    model = buildAsset(kv.Value);
                }
                catch (Exception e) {
                    GD.PrintErr($"Error building {typeof(T)} asset from DTO: {e}");
                    continue;
                }
                if (!isIDOfType(kv.Key)) {
                    GD.PrintErr($"Asset ID {kv.Key} is valid for asset type {typeof(T)}");
                    continue;
                }
                assetDict.Add(kv.Key, model);
            }
        }
    }

    // load initial boat inventories (shape and contents)
    private void LoadDefaultBoatInventories() {
        InventoryDTO? boatInventory = LoadAssetFromJSON<InventoryDTO>(_boatInventoryPath);
        if (boatInventory != null) {
            try {
                // take care not to assign both players the same inventory reference
                Inventory model1 = new Inventory(boatInventory);
                Inventory model2 = new Inventory(boatInventory);
                Inventories.Add(PlayerOneBoatInventoryID, model1);
                Inventories.Add(PlayerTwoBoatInventoryID, model2);
            }
            catch (Exception e) {
                GD.PrintErr($"Error loading boat inventory from DTO: {e}");
            }
        }
    }

    // TODO: validate referential integrity
    // public void ValidateAssetReferrers() {}

    private InventoryItemDefinition GetInventoryItemDefinition(string uuid) {
        try {
            return InventoryItemDefinitions[uuid];
        }
        catch (KeyNotFoundException e) {
            GD.PrintErr($"Inventory item definition not found: {uuid}");
            throw e;
        }
    }
}
