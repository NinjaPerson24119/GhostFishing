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

    private const string _storageInventoryID = "INVENTORY_INSTANCE-9f5d56ed-c8aa-4899-b749-66bd734141fb";

    private const string _fishDefinitionsPath = "res://data/fish.json";
    private const string _itemDefinitionsPath = "res://data/items.json";
    private const string _itemCategoryDefinitionsPath = "res://data/item-categories.json";

    private const string _inventoryDefinitionsPath = "res://data/inventories.json";
    private const string _inventoryInstancesPath = "res://data/inventory-instances.json";

    private const string _questDefinitionsPath = "res://data/quests.json";

    private PlayerStateAssetIDs[] _playerStateAssetIDs;

    private AssetStore<InventoryItemCategoryDTO, InventoryItemCategory> _inventoryItemCategoryStore;
    private AssetStore<InventoryItemDefinitionDTO, InventoryItemDefinition> _inventoryItemDefinitionStore;
    private AssetStore<QuestDefinitionDTO, QuestDefinition> _questDefinitionStore;
    private AssetStore<InventoryDefinitionDTO, InventoryDefinition> _inventoryDefinitionStore;
    private AssetStore<InventoryInstanceDTO, InventoryInstance> _inventoryInstanceStore;

    // stores unglobalized paths to textures
    private Dictionary<string, Texture2D> _persistedTextures = new Dictionary<string, Texture2D>();

    private bool _logLoadedDTOs = true;

    public AssetManager() {
        ProcessMode = ProcessModeEnum.Always;

        _inventoryItemCategoryStore = new AssetStore<InventoryItemCategoryDTO, InventoryItemCategory>(
            buildAssetFromDTO: (string id, InventoryItemCategoryDTO dto) => new InventoryItemCategory(dto),
            isIDOfType: AssetIDUtil.IsInventoryItemCategoryID,
            areDepsSatisfied: null,
            isValidID: AssetIDUtil.IsInventoryItemCategoryID,
            persistedTexturesStore: _persistedTextures
        );
        _questDefinitionStore = new AssetStore<QuestDefinitionDTO, QuestDefinition>(
            buildAssetFromDTO: (string id, QuestDefinitionDTO dto) => new QuestDefinition(dto),
            isIDOfType: AssetIDUtil.IsQuestID,
            areDepsSatisfied: null,
            isValidID: AssetIDUtil.IsQuestID,
            persistedTexturesStore: null
        );
        _inventoryItemDefinitionStore = new AssetStore<InventoryItemDefinitionDTO, InventoryItemDefinition>(
            buildAssetFromDTO: (string id, InventoryItemDefinitionDTO dto) => new InventoryItemDefinition(dto),
            isIDOfType: AssetIDUtil.IsInventoryItemDefinitionID,
            areDepsSatisfied: (InventoryItemDefinition itemDefinition) => AreItemDefinitionsDepsSatisfied(itemDefinition),
            isValidID: AssetIDUtil.IsInventoryItemDefinitionID,
            persistedTexturesStore: _persistedTextures
        );
        _inventoryDefinitionStore = new AssetStore<InventoryDefinitionDTO, InventoryDefinition>(
            buildAssetFromDTO: (string id, InventoryDefinitionDTO dto) => new InventoryDefinition(dto),
            isIDOfType: AssetIDUtil.IsInventoryDefinitionID,
            areDepsSatisfied: null,
            isValidID: AssetIDUtil.IsInventoryDefinitionID,
            persistedTexturesStore: _persistedTextures
        );
        _inventoryInstanceStore = new AssetStore<InventoryInstanceDTO, InventoryInstance>(
            buildAssetFromDTO: (string id, InventoryInstanceDTO dto) => new InventoryInstance(id, dto),
            isIDOfType: AssetIDUtil.IsInventoryInstanceID,
            areDepsSatisfied: (InventoryInstance inventory) => AreInventoryInstanceDepsSatisfied(inventory),
            isValidID: AssetIDUtil.IsInventoryInstanceID,
            persistedTexturesStore: null
        );

        _playerStateAssetIDs = new PlayerStateAssetIDs[2]{
            new PlayerStateAssetIDs(
                boatInventoryInstanceID: "INVENTORY_INSTANCE-31cdec79-2a3b-4f4d-9e23-a878915f3973",
                questInventoryInstanceID: "INVENTORY_INSTANCE-96c151e3-2436-406c-967c-79a1cc89c3ac",
                storageInventoryInstanceID: _storageInventoryID
            ),
            new PlayerStateAssetIDs(
                boatInventoryInstanceID: "INVENTORY_INSTANCE-e158299f-a54e-42b0-964a-3ac732ec3631",
                questInventoryInstanceID: "INVENTORY_INSTANCE-f7091de2-6804-4f22-b8f1-d17be782adf6",
                storageInventoryInstanceID: _storageInventoryID
            )
        };
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);

        GD.Print("Asset Manager: Loading assets...");
        LoadAssets();
        GD.Print("Asset Manager: Done");

        var playerStateView = GetPlayerView(0);
        GD.Print($"Player Default Inventory:\n{playerStateView.BoatInventory.StringRepresentationOfGrid()}");
    }

    private void LoadAssets() {
        // load order is significant
        LoadAssetsFromJSON(_questDefinitionStore, _questDefinitionsPath);
        LoadAssetsFromJSON(_inventoryItemCategoryStore, _itemCategoryDefinitionsPath);
        LoadAssetsFromJSON(_inventoryItemDefinitionStore, _itemDefinitionsPath);
        LoadAssetsFromJSON(_inventoryItemDefinitionStore, _fishDefinitionsPath);
        LoadAssetsFromJSON(_inventoryDefinitionStore, _inventoryDefinitionsPath);
        LoadAssetsFromJSON(_inventoryInstanceStore, _inventoryInstancesPath);
    }

    private bool AreInventoryInstanceDepsSatisfied(InventoryInstance inventory) {
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

    public InventoryDefinition GetInventoryDefinition(string uuid) {
        return _inventoryDefinitionStore.GetAsset(uuid);
    }

    public InventoryInstance GetInventoryInstance(string uuid) {
        return _inventoryInstanceStore.GetAsset(uuid);
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

    public Dictionary<string, InventoryInstanceDTO> GetInventoryInstanceDTOs() {
        return _inventoryInstanceStore.GetAssetDTOs();
    }

    public void SetInventoryInstanceDTOs(Dictionary<string, InventoryInstanceDTO> dtos) {
        foreach (var kv in dtos) {
            try {
                _inventoryInstanceStore.ReplaceAsset(kv.Key, kv.Value);
            }
            catch (Exception e) {
                GD.PrintErr($"Error adding inventory instance DTO with ID {kv.Key}: {e}");
            }
        }
    }
}
