using System.Collections.Generic;
using Godot;

// This class refcounts assets that occur in a spectrum of indices.
// It receives a builder to build the asset for a given index if it doesn't exist.
public class RefCountedAssetSpectrum<Index, T> where Index : notnull {
    public delegate T Builder(Index index);
    private Builder _builder;

    private Dictionary<Index, T> _assets = new Dictionary<Index, T>();
    private Dictionary<Index, int> _assetRefCounts = new Dictionary<Index, int>();

    public RefCountedAssetSpectrum(Builder builder) {
        _builder = builder;
    }

    public T GetAndRef(Index index) {
        if (!_assets.ContainsKey(index)) {
            GD.Print("Building asset for index: ", index);
            _assets[index] = _builder(index);
        }
        AdjustRefCount(index, 1);
        return _assets[index];
    }

    public void Unref(Index index) {
        AdjustRefCount(index, -1);
        DebugTools.Assert(_assetRefCounts[index] >= 0, $"Ref count < 0.");
        if (_assetRefCounts[index] == 0) {
            _assets.Remove(index);
            _assetRefCounts.Remove(index);
        }
    }

    private void AdjustRefCount(Index index, int delta) {
        if (_assetRefCounts.ContainsKey(index)) {
            _assetRefCounts[index] += delta;
        }
        else {
            _assetRefCounts[index] = delta;
        }
    }
}
