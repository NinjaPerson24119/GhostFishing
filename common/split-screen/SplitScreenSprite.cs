using Godot;
using System.Collections.Generic;

public partial class SplitScreenSprite : Sprite2D {
    bool _active = false;
    Texture2D? _playerOneTexture = null;
    Texture2D? _playerTwoTexture = null;
    Vector2 _screenSize = Vector2I.Zero;

    string _shaderPath = "res://common/split-screen/SplitScreenShader.shader";
    private ShaderMaterial _material = new ShaderMaterial();

    public override void _Ready() {
        _material.Shader = GD.Load<Shader>(_shaderPath);
        Material = _material;

        PlayerManager.Ref().CoopChanged += OnCoopChanged;

        Reconfigure(PlayerManager.Ref().CoopActive);
    }

    public void Reconfigure(bool coopActive) {
        _screenSize = GetViewportRect().Size;

        // nodes will have moved, so we need to setup again
        _playerOneTexture = null;
        _playerTwoTexture = null;
        if (coopActive) {
            var players = PlayerInjector.Ref().GetPlayers();
            if (players.ContainsKey(PlayerID.One)) {
                _playerOneTexture = players[PlayerID.One].GetViewport().GetTexture();
            }
            if (players.ContainsKey(PlayerID.Two)) {
                _playerTwoTexture = players[PlayerID.Two].GetViewport().GetTexture();
            }
        }
        Visible = coopActive;
        _active = coopActive;

        ReconfigureShader();
    }

    private void ReconfigureShader() {
        if (_active && _playerOneTexture != null && _playerTwoTexture != null) {
            _material.SetShaderParameter("player_one_texture", _playerOneTexture);
            _material.SetShaderParameter("player_two_texture", _playerTwoTexture);
            _material.SetShaderParameter("screen_size", _screenSize);
        }
    }

    public void OnCoopChanged(bool coopActive) {
        Reconfigure(coopActive);
    }
}
