using Godot;

public partial class SplitScreen : ColorRect {
    [Export]
    public float DividerSize {
        get {
            return _dividerSize;
        }
        set {
            _dividerSize = value;
            ReconfigureShader();
        }
    }
    private float _dividerSize = 0.01f;

    private bool _ready = false;

    private bool _active = false;
    private ViewportTexture? _playerOneTexture = null;
    private ViewportTexture? _playerTwoTexture = null;

    private string _shaderPath = "res://common/split-screen/SplitScreen.gdshader";
    private ShaderMaterial _material = new ShaderMaterial();

    public override void _Ready() {
        Visible = false;

        _material.Shader = GD.Load<Shader>(_shaderPath);
        Material = _material;

        PlayerManager.Ref().CoopChanged += OnCoopChanged;
        _ready = true;

        Reconfigure(PlayerManager.Ref().CoopActive);
    }

    public void Reconfigure(bool coopActive) {
        if (_ready) {
            return;
        }

        _playerOneTexture = null;
        _playerTwoTexture = null;
        if (coopActive) {
            // nodes will have moved, so we need to setup again
            // be sure to get fresh references to players and textures
            var players = PlayerInjector.Ref().GetPlayers();
            if (players.ContainsKey(PlayerID.One)) {
                string subviewportPath = PlayerInjector.Ref().SubViewportPath(PlayerID.One);
                _playerOneTexture = GetNode<SubViewport>(subviewportPath).GetTexture();
            }
            if (players.ContainsKey(PlayerID.Two)) {
                string subviewportPath = PlayerInjector.Ref().SubViewportPath(PlayerID.Two);
                _playerTwoTexture = GetNode<SubViewport>(subviewportPath).GetTexture();
            }
        }
        Size = DisplayServer.WindowGetSize();
        Visible = coopActive;

        ReconfigureShader();
    }

    private void ReconfigureShader() {
        if (_ready) {
            return;
        }

        if (_playerOneTexture != null && _playerTwoTexture != null) {
            _material.SetShaderParameter("divider_size", _dividerSize);
            _material.SetShaderParameter("player_one_texture", _playerOneTexture);
            _material.SetShaderParameter("player_two_texture", _playerTwoTexture);
        }
    }

    public void OnCoopChanged(bool coopActive) {
        Reconfigure(coopActive);
    }
}
