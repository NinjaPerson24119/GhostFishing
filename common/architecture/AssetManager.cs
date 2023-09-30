using Godot;
using System.IO;
using System.Text.Json;

public partial class AssetManager : Node {
    static SingletonTracker<AssetManager> _singletonTracker = new SingletonTracker<AssetManager>();
    private static AssetManager _singleton { get => _singletonTracker.Ref(); }
    public static AssetManager Ref() {
        return _singleton;
    }

    public Inventory DefaultInventory;

    public override void _Ready() {
        _singletonTracker.Ready(this);
        LoadAssets();
    }

    private T LoadJSONFromFile<T>(string filePath) {
        try {
            string globalizedPath = ProjectSettings.GlobalizePath(filePath);
            string jsonString = File.ReadAllText(globalizedPath);
            return JsonSerializer.Deserialize<T>(jsonString);
        }
        catch (FileNotFoundException) {
            GD.PrintErr("File not found: " + filePath);
            return default(T);
        }
    }

    private void LoadAssets() {
        DefaultInventory = LoadJSONFromFile<Inventory>("res://data/default-inventory.json");
    }
}
