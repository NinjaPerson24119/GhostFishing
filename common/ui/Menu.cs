using Godot;
using System.Collections.Generic;

public partial class Menu : Control {
    public List<string> OpenActions { get; private set; } = new List<string>();
    public List<string> CloseActions { get; private set; } = new List<string> { "cancel" };

    public bool IsOpen() {
        return Visible;
    }

    public void Open() {
        GD.Print("Open");
        Visible = true;
    }

    public void Close() {
        Visible = false;
    }
}
