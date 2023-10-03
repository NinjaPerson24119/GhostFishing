using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public class AssetDefinitionArray<T> {
    public T[]? Array { get; set; }
}

public partial class AssetManager : Node {
    static SingletonTracker<AssetManager> _singletonTracker = new SingletonTracker<AssetManager>();
    private static AssetManager _singleton { get => _singletonTracker.Ref(); }
    public static AssetManager Ref() {
        return _singleton;
    }

    private const string _defaultBoatInventoryPath = "res://data/default-boat-inventory.json";
    private const string _defaultQuestInventoryPath = "res://data/default-quest-inventory.json";
    private const string _defaultStorageInventoryPath = "res://data/default-storage-inventory.json";
    private const string _fishDefinitionsPath = "res://data/fish.json";
    private const string _itemCategoryDefinitionsPath = "res://data/item-categories.json";
    private const string _questDefinitionsPath = "res://data/quests.json";

    private const string _temporaryInventoryID = "INVENTORY-dc106fde-f9a0-4a99-8b58-0dee17fce491";
    private PlayerStateAssetIDs[] _playerStateAssetIDs;
    private const string _storageInventoryID = "INVENTORY-9f5d56ed-c8aa-4899-b749-66bd734141fb";

    private InventoryDTO? _defaultBoatInventoryDTO;
    private InventoryDTO? _defaultQuestInventoryDTO;
    private InventoryDTO? _defaultStorageInventoryDTO;
    private Dictionary<string, Inventory> _inventories = new Dictionary<string, Inventory>();
    private Dictionary<string, InventoryItemDefinition> _inventoryItemDefinitions = new Dictionary<string, InventoryItemDefinition>();
    private Dictionary<string, InventoryItemCategory> _inventoryItemCategories = new Dictionary<string, InventoryItemCategory>();
    private Dictionary<string, QuestDefinition> _questDefinitions = new Dictionary<string, QuestDefinition>();

    public AssetManager() {
        ProcessMode = ProcessModeEnum.Always;
        _playerStateAssetIDs = new PlayerStateAssetIDs[2]{
            new PlayerStateAssetIDs(
                boatInventoryID: "INVENTORY-31cdec79-2a3b-4f4d-9e23-a878915f3973",
                questInventoryID: "INVENTORY-96c151e3-2436-406c-967c-79a1cc89c3ac",
                storageID: _storageInventoryID
            ),
            new PlayerStateAssetIDs(
                boatInventoryID: "INVENTORY-e158299f-a54e-42b0-964a-3ac732ec3631",
                questInventoryID: "INVENTORY-f7091de2-6804-4f22-b8f1-d17be782adf6",
                storageID: _storageInventoryID
            )
        };
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
        LoadAssets();
        InitializeInventories();
    }

    private void LoadAssets() {
        _defaultBoatInventoryDTO = LoadAssetFromJSON<InventoryDTO>(_defaultBoatInventoryPath);
        _defaultQuestInventoryDTO = LoadAssetFromJSON<InventoryDTO>(_defaultQuestInventoryPath);
        _defaultStorageInventoryDTO = LoadAssetFromJSON<InventoryDTO>(_defaultStorageInventoryPath);

        LoadAssets(
            _inventoryItemDefinitions,
            _fishDefinitionsPath,
            (InventoryItemDefinitionDTO dto) => new InventoryItemDefinition(dto),
            AssetIDValidator.IsInventoryItemDefinitionID
        );
        LoadAssets(
            _inventoryItemCategories,
            _itemCategoryDefinitionsPath,
            (InventoryItemCategoryDTO dto) => new InventoryItemCategory(dto),
            AssetIDValidator.IsInventoryItemCategoryID
        );
        LoadAssets(_questDefinitions,
            _questDefinitionsPath,
            (QuestDefinitionDTO dto) => new QuestDefinition(dto),
            AssetIDValidator.IsQuestID
        );
    }

    private void InitializeInventories() {
        foreach (PlayerStateAssetIDs ids in _playerStateAssetIDs) {
            if (!_inventories.ContainsKey(ids.BoatInventoryID) && _defaultBoatInventoryDTO != null) {
                _inventories.Add(ids.BoatInventoryID, new Inventory(_defaultBoatInventoryDTO));
            }
            if (!_inventories.ContainsKey(ids.QuestInventoryID) && _defaultQuestInventoryDTO != null) {
                _inventories.Add(ids.QuestInventoryID, new Inventory(_defaultQuestInventoryDTO));
            }
            if (!_inventories.ContainsKey(ids.StorageID) && _defaultStorageInventoryDTO != null) {
                _inventories.Add(ids.StorageID, new Inventory(_defaultStorageInventoryDTO));
            }
        }
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

    // TODO: validate referential integrity
    // public void ValidateAssetReferrers() {}

    public PlayerStateView GetPlayerView(int playerIndex) {
        if (playerIndex < 0 || playerIndex >= _playerStateAssetIDs.Length) {
            throw new IndexOutOfRangeException($"Player index {playerIndex} is out of range");
        }
        PlayerStateAssetIDs assetIDs = _playerStateAssetIDs[playerIndex];
        return new PlayerStateView(assetIDs);
    }

    public Inventory GetInventory(string uuid) {
        try {
            return _inventories[uuid];
        }
        catch (KeyNotFoundException e) {
            GD.PrintErr($"Inventory not found: {uuid}");
            throw e;
        }
    }

    public InventoryItemDefinition GetInventoryItemDefinition(string uuid) {
        try {
            return _inventoryItemDefinitions[uuid];
        }
        catch (KeyNotFoundException e) {
            GD.PrintErr($"Inventory item definition not found: {uuid}");
            throw e;
        }
    }
}
