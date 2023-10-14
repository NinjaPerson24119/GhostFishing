using System;
using System.Collections.Generic;
using Godot;

internal class AssetStore<DTO, T> where DTO : IGameAssetDTO {
    private Dictionary<string, T> _assets = new Dictionary<string, T>();

    public delegate T BuildAssetFromDTO(DTO dto);
    public delegate bool IsIDOfType(string id);
    public delegate bool AreDepsSatisfied(T asset);
    public delegate bool IsValidID(string id);
    private BuildAssetFromDTO _buildAssetFromDTO;
    private IsIDOfType _isIDOfType;
    private AreDepsSatisfied? _areDepsSatisfied;
    private IsValidID _isValidID;

    private Dictionary<string, Texture2D>? _persistedTexturesStore;

    public AssetStore(
        BuildAssetFromDTO buildAssetFromDTO,
        IsIDOfType isIDOfType,
        AreDepsSatisfied? areDepsSatisfied,
        IsValidID isValidID,
        Dictionary<string, Texture2D>? persistedTexturesStore
    ) {
        _buildAssetFromDTO = buildAssetFromDTO;
        _isIDOfType = isIDOfType;
        _areDepsSatisfied = areDepsSatisfied;
        _isValidID = isValidID;
        _persistedTexturesStore = persistedTexturesStore;
    }

    public void AddAsset(string id, DTO dto) {
        GD.Print($"Adding {typeof(T)} asset with ID: {id}");

        if (!_isValidID(id)) {
            throw new ArgumentException($"Invalid {typeof(T)} asset type ID: {id}");
        }
        if (dto == null) {
            GD.PrintErr($"DTO is null for {id} with asset type {typeof(T)}");
            return;
        }
        T model;
        try {
            model = _buildAssetFromDTO(dto);
        }
        catch (Exception e) {
            GD.PrintErr($"Error building {typeof(T)} asset from DTO: {e}");
            return;
        }
        if (!_isIDOfType(id)) {
            GD.PrintErr($"Asset ID {id} is valid for asset type {typeof(T)}");
            return;
        }
        if (_areDepsSatisfied != null && !_areDepsSatisfied(model)) {
            GD.PrintErr($"Dependencies not satisfied for {id} with asset type {typeof(T)}");
            return;
        }

        var dtoWithImages = dto as IGameAssetDTOWithImages;
        if (dtoWithImages != null && _persistedTexturesStore != null) {
            string[] imageAssetPaths = dtoWithImages.ImageAssetPaths();
            foreach (string imageAssetPath in imageAssetPaths) {
                if (_persistedTexturesStore.ContainsKey(imageAssetPath)) {
                    continue;
                }
                string globalizedPath = ProjectSettings.GlobalizePath(imageAssetPath);
                try {
                    Texture2D texture = GD.Load<Texture2D>(globalizedPath);
                    GD.Print($"Loaded texture from path {globalizedPath}");
                    _persistedTexturesStore.Add(imageAssetPath, texture);
                }
                catch (Exception e) {
                    GD.PrintErr($"Error loading texture from path {globalizedPath}: {e}");
                }
            }
        }

        _assets.Add(id, model);
    }

    public T GetAsset(string id) {
        if (!_isValidID(id)) {
            throw new ArgumentException($"Invalid {typeof(T)} asset type ID: {id}");
        }
        try {
            return _assets[id];
        }
        catch (KeyNotFoundException e) {
            GD.PrintErr($"{typeof(T)} definition not found for {id}");
            throw e;
        }
    }

    public bool HasAsset(string id) {
        if (!_isValidID(id)) {
            throw new ArgumentException($"Invalid {typeof(T)} asset type ID: {id}");
        }
        return _assets.ContainsKey(id);
    }
}
