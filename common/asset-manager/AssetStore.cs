using System;
using System.Collections.Generic;
using Godot;

internal class AssetStore<DTO, T> where DTO : IGameAssetDTO {
    private Dictionary<string, T> _assets = new Dictionary<string, T>();

    // this should not have a second ID argument
    // if an object needs to be referred by ID, it should be an instance variant with an ID field
    public delegate T BuildAssetFromDTO(string id, DTO dto);
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
        GD.Print($"\n---\nAdding {typeof(T)} asset with ID: {id}");

        if (!_isValidID(id)) {
            throw new ArgumentException($"Invalid {typeof(T)} asset type ID: {id}");
        }
        if (dto == null) {
            GD.PrintErr($"DTO is null for {id} with asset type {typeof(T)}");
            return;
        }
        if (!string.IsNullOrEmpty(id) && !_isValidID(id)) {
            GD.PrintErr($"Invalid {typeof(T)} asset type ID: {id}");
            return;
        }
        T model;
        try {
            model = _buildAssetFromDTO(id, dto);
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

    public void ReplaceAsset(string id, DTO dto) {
        if (_assets.ContainsKey(id)) {
            _assets.Remove(id);
        }
        AddAsset(id, dto);
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

    public Dictionary<string, DTO> GetAssetDTOs() {
        Dictionary<string, DTO> dtos = new Dictionary<string, DTO>();
        if (!typeof(IGameAssetWritable<DTO>).IsAssignableFrom(typeof(T))) {
            throw new ArgumentException($"Asset type {typeof(T)} does not implement IGameAssetWritable");
        }
        foreach (var kv in _assets) {
            if (!(kv.Value as IGameAssetWritable<DTO>)!.IsTouched()) {
                continue;
            }
            DTO dto = (kv.Value as IGameAssetWritable<DTO>)!.ToDTO();
            dtos.Add(kv.Key, dto);
        }
        return dtos;
    }
}
