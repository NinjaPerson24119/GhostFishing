using System;
using System.Collections.Generic;
using Godot;

public class AssetStore<DTO, T> where DTO : IGameAssetDTO {
    private Dictionary<string, T> _assets = new Dictionary<string, T>();

    public delegate T BuildAssetFromDTO(DTO dto);
    public delegate bool IsIDOfType(string id);
    public delegate bool AreDepsSatisfied(T asset);
    public delegate bool IsValidID(string id);
    private BuildAssetFromDTO _buildAssetFromDTO;
    private IsIDOfType _isIDOfType;
    private AreDepsSatisfied? _areDepsSatisfied;
    private IsValidID _isValidID;

    public AssetStore(BuildAssetFromDTO buildAssetFromDTO, IsIDOfType isIDOfType, AreDepsSatisfied? areDepsSatisfied, IsValidID isValidID) {
        _buildAssetFromDTO = buildAssetFromDTO;
        _isIDOfType = isIDOfType;
        _areDepsSatisfied = areDepsSatisfied;
        _isValidID = isValidID;
    }

    public void AddAsset(string id, DTO dto) {
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
