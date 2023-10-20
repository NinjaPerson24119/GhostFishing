using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class PlayerInjector : Node {
    static SingletonTracker<PlayerInjector> _singletonTracker = new SingletonTracker<PlayerInjector>();
    private static PlayerInjector _singleton { get => _singletonTracker.Ref(); }
    public static PlayerInjector Ref() {
        return _singleton;
    }

    private Dictionary<PlayerID, Player>? _players;
    // node that the players or their viewports are children of
    private Node? _playersWorkingParent;

    public PlayerInjector() {
        ProcessMode = ProcessModeEnum.Always;
    }
    private bool _usingSubviewports = false;

    // split-screen cannot depend directly on PlayerManager since injector will change tree
    // use a chained signal to notify split-screen of changes once the tree updates are done
    [Signal]
    public delegate void SplitScreenChangedEventHandler(bool splitScreenActive);

    public override void _Ready() {
        _singletonTracker.Ready(this);
        _playersWorkingParent = GetNode("/root/Main/Pausable");
        PlayerManager.Ref().CoopChanged += OnCoopChanged;

        RebuildPlayers();
    }

    public Dictionary<PlayerID, Player> GetPlayers() {
        if (_players == null) {
            throw new System.Exception("PlayerInjector not ready");
        }
        return _players;
    }

    private void RebuildPlayers() {
        _players = new Dictionary<PlayerID, Player>();
        foreach (var playerID in PlayerManager.PlayerIDs) {
            switch (playerID) {
                case PlayerID.One:
                    _players.Add(playerID, GetPlayerOne());
                    break;
                case PlayerID.Two:
                    _players.Add(playerID, GetPlayerTwo());
                    break;
                default:
                    throw new System.Exception($"Unknown player ID {playerID}");
            }
        }
    }

    private string InactivePlayerName(PlayerID playerID) {
        return $"InactivePlayer-{playerID.PlayerNumber()}";
    }
    private string InactivePlayerPath(PlayerID playerID) {
        return $"/root/Main/Pausable/{InactivePlayerName(playerID)}";
    }
    private string PlayerContextName(PlayerID playerID) {
        return $"PlayerContext-{playerID.PlayerNumber()}";
    }
    private string SinglePlayerContextPath(PlayerID playerID) {
        return $"/root/Main/Pausable/{PlayerContextName(playerID)}";
    }
    private string SubViewportName(PlayerID playerID) {
        return $"SubViewport-{playerID.PlayerNumber()}";
    }
    public string SubViewportPath(PlayerID playerID) {
        return $"/root/Main/Pausable/{SubViewportName(playerID)}";
    }
    private string CoopPlayerContextPath(PlayerID playerID) {
        return SubViewportPath(playerID) + $"/{PlayerContextName(playerID)}";
    }

    public PlayerContext GetPlayerOneContext() {
        if (_usingSubviewports) {
            //GD.PrintErr($"GetPlayerOneContext: {CoopPlayerContextPath(PlayerID.One)}");
            return GetNode<PlayerContext>(CoopPlayerContextPath(PlayerID.One));
        }
        else {
            //GD.PrintErr($"GetPlayerOneContext: {SinglePlayerContextPath(PlayerID.One)}");
            return GetNode<PlayerContext>(SinglePlayerContextPath(PlayerID.One));
        }
    }

    public Player GetPlayerOne() {
        return GetPlayerOneContext().Player;
    }

    private Player GetPlayerTwo() {
        if (_usingSubviewports) {
            GD.Print($"(player injector) GetPlayerTwo (using subviewports): {CoopPlayerContextPath(PlayerID.Two)}");
            PlayerContext playerContext = GetNode<PlayerContext>(CoopPlayerContextPath(PlayerID.Two));
            return playerContext.Player;
        }
        else {
            return GetNode<Player>(InactivePlayerPath(PlayerID.Two));
        }
    }

    public void OnCoopChanged(bool coopActive) {
        GD.Print($"(player injector) Split-screen coop changed, {coopActive}");
        if (_players == null) {
            throw new System.Exception("PlayerInjector not ready");
        }
        if (coopActive == _usingSubviewports) {
            throw new System.Exception("Cannot change coop state to current state as viewport mutation is not idempotent");
        }
        if (_playersWorkingParent == null) {
            throw new System.Exception("Players working parent is null");
        }

        // for both players, set player as subtree of subviewport (or not)
        foreach (var kv in _players) {
            var playerID = kv.Key;
            var player = kv.Value;

            if (playerID == PlayerID.One && player.PlayerContext == null) {
                throw new System.Exception("Player one PlayerContext should never be null");
            }

            // for Player 2, swap `Player` with `PlayerContext` (and vice versa)
            // missing PlayerContext implies we have an inactive player
            if (coopActive) {
                // activate co-op
                GD.Print($"(player injector) Activating player {playerID}");
                if (playerID == PlayerID.One) {
                    GD.Print($"(player injector) Moving Player One to subviewport");
                    SubViewport subViewport = CreateSubViewport(playerID);
                    MoveNode(SinglePlayerContextPath(playerID), subViewport);
                    _playersWorkingParent.AddChild(subViewport);
                    continue;
                }
                if (player.PlayerContext == null) {
                    GD.Print($"(player injector) Player {playerID} is inactive, replacing with active player");
                    ReplaceInactivePlayerWithSubViewportAndPlayerContext(playerID);
                    continue;
                }
                throw new System.Exception("Player inactive but PlayerContext exists");
            }
            else {
                // deactivate co-op
                GD.Print($"(player injector) Deactivating player {playerID}");
                if (playerID == PlayerID.One) {
                    GD.Print($"(player injector) Moving Player One to root");
                    SubViewport subViewport = GetNode<SubViewport>(SubViewportPath(playerID));
                    MoveNode(CoopPlayerContextPath(playerID), subViewport.GetParent());
                    subViewport.QueueFree();
                    continue;
                }
                if (player.PlayerContext != null) {
                    GD.Print($"(player injector) Player {playerID} is active, replacing with inactive player");
                    ReplaceSubViewportAndPlayerContextWithInactivePlayer(playerID);
                    continue;
                }
                throw new System.Exception("Player active but PlayerContext does not exist");
            }
        }

        _usingSubviewports = coopActive;
        RebuildPlayers();
        EmitSignal(SignalName.SplitScreenChanged, _usingSubviewports);
    }

    private SubViewport CreateSubViewport(PlayerID playerID) {
        PackedScene subviewportScene = ResourceLoader.Load<PackedScene>("res://common/split-screen/SplitScreenSubViewport.tscn");
        SubViewport? subViewport = subviewportScene.Instantiate() as SubViewport;
        if (subViewport == null) {
            throw new System.Exception("Failed to instantiate subviewport");
        }
        subViewport.Name = SubViewportName(playerID);
        Vector2 viewportSize = DisplayServer.WindowGetSize();
        subViewport.Size = new Vector2I(Mathf.FloorToInt(viewportSize.X / 2), Mathf.FloorToInt(viewportSize.Y));

        return subViewport;
    }

    private void ReplaceInactivePlayerWithSubViewportAndPlayerContext(PlayerID playerID) {
        if (_players == null) {
            throw new System.Exception("PlayerInjector not ready");
        }
        Transform3D transform3D = _players[playerID].GlobalTransform;
        RemoveNode(InactivePlayerPath(playerID));
        PackedScene playerCtxScene = ResourceLoader.Load<PackedScene>("res://common/player/PlayerContext.tscn");
        PlayerContext? playerCtx = playerCtxScene.Instantiate() as PlayerContext;
        if (playerCtx == null) {
            throw new System.Exception("Failed to instantiate player context");
        }
        playerCtx.Name = PlayerContextName(playerID);
        playerCtx.PlayerID = playerID;
        playerCtx.Player.GlobalTransform = transform3D;

        SubViewport subViewport = CreateSubViewport(playerID);
        subViewport.AddChild(playerCtx);

        if (_playersWorkingParent == null) {
            throw new System.Exception("Players working parent is null");
        }
        _playersWorkingParent.AddChild(subViewport);
    }

    private void ReplaceSubViewportAndPlayerContextWithInactivePlayer(PlayerID playerID) {
        if (_players == null) {
            throw new System.Exception("PlayerInjector not ready");
        }
        if (!_usingSubviewports) {
            throw new System.Exception("Cannot replace subviewport and player context with inactive player when not using subviewports");
        }

        Transform3D globalTransform = _players[playerID].GlobalTransform;
        RemoveNode(CoopPlayerContextPath(playerID));
        PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://common/player/Player.tscn");
        Player? player = playerScene.Instantiate() as Player;
        if (player == null) {
            throw new System.Exception("Failed to instantiate player");
        }
        player.Name = InactivePlayerName(playerID);
        player.PlayerID = playerID;
        player.GlobalTransform = globalTransform;

        if (_playersWorkingParent == null) {
            throw new System.Exception("Players working parent is null");
        }
        _playersWorkingParent.AddChild(player);
    }

    private void RemoveNode(string nodePath) {
        var node = GetNode(nodePath);
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }

    private void MoveNode(string nodePath, Node newParent) {
        var node = GetNode(nodePath);
        node.GetParent().RemoveChild(node);
        newParent.AddChild(node);
    }
}
