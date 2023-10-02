using System.Text.Json.Serialization;
using Godot;

public enum InventoryItemRotation {
    None = 0,
    Clockwise90 = 1,
    Clockwise180 = 2,
    Clockwise270 = 3,
}

public class InventoryItemSpatial {
    public int Y { get; set; }
    public int X { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public InventoryItemRotation Rotation = InventoryItemRotation.None;

    [JsonPropertyName("FilledMask")]
    public bool[] SerializedFilledMask {
        get {
            return _filledMaskClockwise0;
        }
        set {
            _filledMaskClockwise0 = value;
            Matrix<bool> filledMaskMatrix = new Matrix<bool>() {
                Width = Width,
                Height = Height,
                Data = value,
            };

            // Clone() is shallow copy, but bool is not a reference type, so it's OK
            filledMaskMatrix.RotateClockwise90();
            _filledMaskClockwise90 = filledMaskMatrix.Data.Clone() as bool[];
            filledMaskMatrix.RotateClockwise90();
            _filledMaskClockwise180 = filledMaskMatrix.Data.Clone() as bool[];
            filledMaskMatrix.RotateClockwise90();
            _filledMaskClockwise270 = filledMaskMatrix.Data.Clone() as bool[];
        }
    }
    private bool[] _filledMaskClockwise0 { get; set; }
    private bool[] _filledMaskClockwise90;
    private bool[] _filledMaskClockwise180;
    private bool[] _filledMaskClockwise270;
    public bool[] GetFilledMask() {
        switch (Rotation) {
            case InventoryItemRotation.None:
                return _filledMaskClockwise0;
            case InventoryItemRotation.Clockwise90:
                return _filledMaskClockwise90;
            case InventoryItemRotation.Clockwise180:
                return _filledMaskClockwise180;
            case InventoryItemRotation.Clockwise270:
                return _filledMaskClockwise270;
            default:
                throw new System.ArgumentException("Invalid rotation");
        }
    }

    public override string ToString() {
        string str = $"X: {X}, Y: {Y}, Width: {Width}, Height: {Height}, Rotation: {Rotation}\n";
        if (SerializedFilledMask != null) {
            str += "FilledMask:\n";
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    str += GetFilledMask()[y * Width + x] ? "1" : "0";
                }
                str += "\n";
            }
        }
        return str;
    }
}

public class InventoryItem {
    public string UUID { get; set; }
    public InventoryItemSpatial Spatial { get; set; }
}

