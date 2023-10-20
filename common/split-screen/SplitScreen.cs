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
    private float _dividerSize = 0.001f;

    private bool _ready = false;

    private bool _active = false;
    private ViewportTexture? _playerOneTexture = null;
    private ViewportTexture? _playerTwoTexture = null;

    private string _shaderPath = "res://common/split-screen/SplitScreen.gdshader";
    private ShaderMaterial _material = new ShaderMaterial();

    private Timer _splashTimer = new Timer() {
        WaitTime = 0.5f,
        OneShot = true,
    };

    public override void _Ready() {
        Visible = false;

        _material.Shader = GD.Load<Shader>(_shaderPath);
        Material = _material;

        _splashTimer.Timeout += OnSplashTimeout;
        AddChild(_splashTimer);

        PlayerInjector.Ref().SplitScreenChanged += OnSplitScreenChanged;
        _ready = true;

        Reconfigure(PlayerManager.Ref().CoopActive);
    }

    public void Reconfigure(bool splitScreenActive) {
        if (!_ready) {
            return;
        }

        _playerOneTexture = null;
        _playerTwoTexture = null;
        if (splitScreenActive) {
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
        _splashTimer.Start();
        DependencyInjector.Ref().GetSplashScreen().Visible = true;
        Visible = splitScreenActive;

        GD.Print($"(split-screen) Reconfigured, {splitScreenActive}");
        ReconfigureShader();
    }

    private void ReconfigureShader() {
        if (!_ready) {
            return;
        }

        if (_playerOneTexture != null && _playerTwoTexture != null) {
            _material.SetShaderParameter("divider_size", _dividerSize);
            _material.SetShaderParameter("player_one_texture", _playerOneTexture);
            _material.SetShaderParameter("player_two_texture", _playerTwoTexture);
        }
        GD.Print("(split-screen) Reconfigured shader");
    }

    public void OnSplitScreenChanged(bool splitScreenActive) {
        GD.Print($"(split-screen) Split-screen coop changed, {splitScreenActive}");
        Reconfigure(splitScreenActive);
        DependencyInjector.Ref().GetPauseMenu().RequestClose();
    }

    public void OnSplashTimeout() {
        DependencyInjector.Ref().GetSplashScreen().Visible = false;
    }
}
