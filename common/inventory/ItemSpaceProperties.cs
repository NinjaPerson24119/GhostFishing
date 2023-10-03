public enum InventoryItemRotation {
    None = 0,
    Clockwise90 = 1,
    Clockwise180 = 2,
    Clockwise270 = 3,
}

public class InventoryItemSpacePropertiesDTO : IGameAssetDTO {
    public int Width { get; set; }
    public int Height { get; set; }
    public bool[]? FilledMask { get; set; }

    public bool Validate() {
        if (Width <= 0 || Height <= 0) {
            return false;
        }
        if (FilledMask == null) {
            return false;
        }
        if (FilledMask.Length != Width * Height) {
            return false;
        }
        if (!ConnectedArray.IsArrayConnected(Width, Height, FilledMask)) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        string str = $"Width: {Width}, Height: {Height}\n";
        if (FilledMask != null) {
            str += "FilledMask:\n";
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    str += FilledMask[y * Width + x] ? "1" : "0";
                }
                str += "\n";
            }
        }
        return str;
    }
}

public class InventoryItemSpaceProperties {
    public int Width { get; private set; }
    public int Height { get; private set; }
    private bool[] _filledMask {
        get {
            return _filledMaskClockwise0;
        }
        set {
            _filledMaskClockwise0 = value;
            Matrix<bool> filledMaskMatrix = new Matrix<bool>(Width, Height, value);

            // Clone() is shallow copy, but bool is not a reference type, so it's OK
            filledMaskMatrix.RotateClockwise90();
            var clone = filledMaskMatrix.Data.Clone() as bool[];
            if (clone == null) {
                throw new System.Exception("Clone() failed");
            }
            _filledMaskClockwise90 = clone;

            filledMaskMatrix.RotateClockwise90();
            clone = filledMaskMatrix.Data.Clone() as bool[];
            if (clone == null) {
                throw new System.Exception("Clone() failed");
            }
            _filledMaskClockwise180 = clone;

            filledMaskMatrix.RotateClockwise90();
            clone = filledMaskMatrix.Data.Clone() as bool[];
            if (clone == null) {
                throw new System.Exception("Clone() failed");
            }
            _filledMaskClockwise270 = clone;
        }
    }
    private bool[] _filledMaskClockwise0 { get; set; }
    private bool[] _filledMaskClockwise90;
    private bool[] _filledMaskClockwise180;
    private bool[] _filledMaskClockwise270;

    public InventoryItemSpaceProperties(InventoryItemSpacePropertiesDTO dto) {
        Width = dto.Width;
        Height = dto.Height;
        _filledMask = dto.FilledMask ?? new bool[Width * Height];
    }

    public bool[] GetFilledMask(InventoryItemRotation rotation) {
        switch (rotation) {
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
}
