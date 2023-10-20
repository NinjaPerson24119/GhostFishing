using System.Linq;
using System;
using Godot;

public class InventoryDefinitionDTO : IGameAssetDTO, IGameAssetDTOWithImages {
    public int Width { get; set; }
    public int Height { get; set; }
    public string? BackgroundImagePath { get; set; }
    // disabled is used when an inventory needs to exist, but shouldn't be interactive
    // - inspecting the contents of a locked chest
    // - displaying a completed crafting result
    public bool[]? UsableMask { get; set; }

    public bool IsValid() {
        if (Width <= 0 || Height <= 0) {
            GD.PrintErr($"Invalid inventory size: {Width}x{Height}");
            return false;
        }
        if (UsableMask != null) {
            if (UsableMask.Length != Width * Height) {
                GD.PrintErr($"Invalid usable mask size: {UsableMask.Length} != {Width * Height}");
                return false;
            }
            if (!ConnectedArray.IsArrayConnected(Width, Height, UsableMask)) {
                GD.PrintErr($"Invalid usable mask: not connected");
                return false;
            }
        }

        return true;
    }

    public string Stringify() {
        string str = $"Width: {Width}\nHeight: {Height}\n";
        if (BackgroundImagePath != null) {
            str += $"BackgroundImagePath: {BackgroundImagePath}\n";
        }
        if (UsableMask != null) {
            str += "UsableMask:\n";
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    str += UsableMask[y * Width + x] ? "1" : "0";
                }
                str += "\n";
            }
        }
        return str;
    }

    public string[] ImageAssetPaths() {
        if (string.IsNullOrEmpty(BackgroundImagePath)) {
            return new string[] { };
        }
        return new string[] { BackgroundImagePath };
    }
}

public class InventoryDefinition {
    public int Width { get; set; }
    public int Height { get; set; }
    public string? BackgroundImagePath { get; set; }
    // indicate spaces that are usable (shape might not be a perfect rectangle)
    public bool[] UsableMask;

    public InventoryDefinition(InventoryDefinitionDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryDefinitionDTO");
        }
        Width = dto.Width;
        Height = dto.Height;
        BackgroundImagePath = dto.BackgroundImagePath;
        if (dto.UsableMask != null) {
            UsableMask = dto.UsableMask;
        }
        else {
            UsableMask = Enumerable.Repeat(true, Width * Height).ToArray();
        }
    }

    public InventoryDefinition(int width, int height) {
        Width = width;
        Height = height;
        UsableMask = Enumerable.Repeat(true, Width * Height).ToArray();
    }
}
