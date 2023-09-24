using Godot;

public partial class Main : Node {
    public override void _Ready() {
        // setup signals
        GetNode<Player>("Player").PositionChangedSignificantly += GetNode<Ocean>("Ocean").OnOriginChanged;
    }
}
