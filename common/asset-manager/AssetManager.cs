using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

internal class AssetDefinitionArray<T> {
    public T[]? Array { get; set; }
}

internal partial class AssetManager : Node {
    static SingletonTracker<AssetManager> _singletonTracker = new SingletonTracker<AssetManager>();
    private static AssetManager _singleton { get => _singletonTracker.Ref(); }
    public static AssetManager Ref() {
        return _singleton;
    }

    private const string _defaultBoatInventoryPath = "res://data/default-boat-inventory.json";
    private const string _defaultQuestInventoryPath = "res://data/default-quest-inventory.json";
    private const string _defaultStorageInventoryPath = "res://data/default-storage-inventory.json";
    private const string _fishDefinitionsPath = "res://data/fish.json";
    private const string _itemDefinitionsPath = "res://data/items.json";
    private const string _itemCategoryDefinitionsPath = "res://data/item-categories.json";
    private const string _questDefinitionsPath = "res://data/quests.json";

    private PlayerStateAssetIDs[] _playerStateAssetIDs;
    private const string _storageInventoryID = "INVENTORY-9f5d56ed-c8aa-4899-b749-66bd734141fb";

    private InventoryDTO? _defaultBoatInventoryDTO;
    private InventoryDTO? _defaultQuestInventoryDTO;
    private InventoryDTO? _defaultStorageInventoryDTO;

    private AssetStore<InventoryItemCategoryDTO, InventoryItemCategory> _inventoryItemCategoryStore;
    private AssetStore<InventoryItemDefinitionDTO, InventoryItemDefinition> _inventoryItemDefinitionStore;
    private AssetStore<QuestDefinitionDTO, QuestDefinition> _questDefinitionStore;
    private AssetStore<InventoryDTO, Inventory> _inventoryStore;
    // stores unglobalized paths to textures
    private Dictionary<string, Texture2D> _persistedTextures = new Dictionary<string, Texture2D>();

    private bool _logLoadedDTOs = true;

    public AssetManager() {
        ProcessMode = ProcessModeEnum.Always;

        _inventoryItemCategoryStore = new AssetStore<InventoryItemCategoryDTO, InventoryItemCategory>(
            (InventoryItemCategoryDTO dto) => new InventoryItemCategory(dto),
            AssetIDUtil.IsInventoryItemCategoryID,
            null,
            AssetIDUtil.IsInventoryItemCategoryID,
            _persistedTextures
        );
        _questDefinitionStore = new AssetStore<QuestDefinitionDTO, QuestDefinition>(
            (QuestDefinitionDTO dto) => new QuestDefinition(dto),
            AssetIDUtil.IsQuestID,
            null,
            AssetIDUtil.IsQuestID,
            _persistedTextures
        );
        _inventoryItemDefinitionStore = new AssetStore<InventoryItemDefinitionDTO, InventoryItemDefinition>(
            (InventoryItemDefinitionDTO dto) => new InventoryItemDefinition(dto),
            AssetIDUtil.IsInventoryItemDefinitionID,
            (InventoryItemDefinition itemDefinition) => AreItemDefinitionsDepsSatisfied(itemDefinition),
            AssetIDUtil.IsInventoryItemDefinitionID,
            _persistedTextures
        );
        _inventoryStore = new AssetStore<InventoryDTO, Inventory>(
            (InventoryDTO dto) => new Inventory(dto),
            AssetIDUtil.IsInventoryID,
            (Inventory inventory) => AreInventoryDepsSatisfied(inventory),
            AssetIDUtil.IsInventoryID,
            _persistedTextures
        );

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

        GD.Print("Asset Manager: Loading assets...");
        LoadAssets();
        GD.Print("Asset Manager: Initializing inventories...");
        InitializeInventories();
        GD.Print("Asset Manager: Done");

        var playerStateView = GetPlayerView(0);
        GD.Print($"Player Default Inventory:\n{playerStateView.BoatInventory.StringRepresentationOfGrid()}");
    }

    private void LoadAssets() {
        // load order is significant
        LoadAssetsFromJSON(_inventoryItemCategoryStore, _itemCategoryDefinitionsPath);
        LoadAssetsFromJSON(_questDefinitionStore, _questDefinitionsPath);
        LoadAssetsFromJSON(_inventoryItemDefinitionStore, _itemDefinitionsPath);
        LoadAssetsFromJSON(_inventoryItemDefinitionStore, _fishDefinitionsPath);

        _defaultBoatInventoryDTO = LoadAssetFromJSON<InventoryDTO>(_defaultBoatInventoryPath);
        _defaultQuestInventoryDTO = LoadAssetFromJSON<InventoryDTO>(_defaultQuestInventoryPath);
        _defaultStorageInventoryDTO = LoadAssetFromJSON<InventoryDTO>(_defaultStorageInventoryPath);
        if (_logLoadedDTOs) {
            LogDTO("Default boat inventory", _defaultBoatInventoryDTO);
            LogDTO("Default quest inventory", _defaultQuestInventoryDTO);
            LogDTO("Default storage inventory", _defaultStorageInventoryDTO);
        }
    }

    private bool AreInventoryDepsSatisfied(Inventory inventory) {
        foreach (InventoryItemInstance item in inventory.Items) {
            if (!AreItemInstanceDepsSatisfied(item)) {
                return false;
            }
        }
        return true;
    }

    private bool AreItemDefinitionsDepsSatisfied(InventoryItemDefinition itemDefinition) {
        if (itemDefinition.CategoryID != null && !_inventoryItemCategoryStore.HasAsset(itemDefinition.CategoryID)) {
            return false;
        }
        return true;
    }

    private bool AreItemInstanceDepsSatisfied(InventoryItemInstance itemInstance) {
        if (!_inventoryItemDefinitionStore.HasAsset(itemInstance.ItemDefinitionID)) {
            return false;
        }
        return true;
    }

    private void InitializeInventories() {
        foreach (PlayerStateAssetIDs ids in _playerStateAssetIDs) {
            if (!_inventoryStore.HasAsset(ids.BoatInventoryID) && _defaultBoatInventoryDTO != null) {
                _inventoryStore.AddAsset(ids.BoatInventoryID, _defaultBoatInventoryDTO);
            }
            if (!_inventoryStore.HasAsset(ids.QuestInventoryID) && _defaultQuestInventoryDTO != null) {
                _inventoryStore.AddAsset(ids.QuestInventoryID, _defaultQuestInventoryDTO);
            }
            if (!_inventoryStore.HasAsset(ids.StorageID) && _defaultStorageInventoryDTO != null) {
                _inventoryStore.AddAsset(ids.StorageID, _defaultStorageInventoryDTO);
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

    private void LoadAssetsFromJSON<DTO, T>(AssetStore<DTO, T> store, string filePath) where DTO : IGameAssetDTO {
        Dictionary<string, DTO>? assetDTOs = LoadAssetFromJSON<Dictionary<string, DTO>>(filePath);
        if (assetDTOs != null) {
            foreach (var kv in assetDTOs) {
                if (_logLoadedDTOs) {
                    LogDTO(kv.Key, kv.Value);
                }
                store.AddAsset(kv.Key, kv.Value);
            }
        }
    }

    private void LogDTO<DTO>(string id, DTO? dto) where DTO : IGameAssetDTO {
        if (dto == null) {
            GD.PrintErr($"DTO is null for {id} ({typeof(DTO)})");
            return;
        }
        GD.Print($"Loaded Asset: {id} ({typeof(DTO)}):\n{dto.Stringify()}\n\n");
    }

    public PlayerStateView GetPlayerView(int playerIndex) {
        if (playerIndex < 0 || playerIndex >= _playerStateAssetIDs.Length) {
            throw new IndexOutOfRangeException($"Player index {playerIndex} is out of range");
        }
        PlayerStateAssetIDs assetIDs = _playerStateAssetIDs[playerIndex];
        return new PlayerStateView(assetIDs);
    }

    public Inventory GetInventory(string uuid) {
        return _inventoryStore.GetAsset(uuid);
    }

    public InventoryItemDefinition GetInventoryItemDefinition(string uuid) {
        return _inventoryItemDefinitionStore.GetAsset(uuid);
    }

    public InventoryItemCategory GetInventoryCategory(string uuid) {
        return _inventoryItemCategoryStore.GetAsset(uuid);
    }

    public void PersistImage(string imagePath) {
        if (_persistedTextures.ContainsKey(imagePath)) {
            return;
        }
        string globalizedPath = ProjectSettings.GlobalizePath(imagePath);
        Texture2D texture = GD.Load<Texture2D>(globalizedPath);
        _persistedTextures.Add(imagePath, texture);
        GD.Print($"Persisted texture from path {globalizedPath}");
    }
}
