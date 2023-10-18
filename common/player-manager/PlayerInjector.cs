using Godot;
using System.Collections.Generic;

public partial class PlayerInjector : Node {
    static SingletonTracker<PlayerInjector> _singletonTracker = new SingletonTracker<PlayerInjector>();
    private static PlayerInjector _singleton { get => _singletonTracker.Ref(); }
    public static PlayerInjector Ref() {
        return _singleton;
    }

    private Dictionary<PlayerID, Player>? _players;

    public PlayerInjector() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);

        _players = new Dictionary<PlayerID, Player>();
        foreach (var playerID in PlayerManager.PlayerIDs) {
            switch (playerID) {
                case PlayerID.One:
                    _players.Add(playerID, GetPlayerOne());
                    break;
                case PlayerID.Two:
                    _players.Add(playerID, GetPlayerOne());
                    break;
                default:
                    throw new System.Exception($"Unknown player ID {playerID}");
            }
        }
    }

    public Dictionary<PlayerID, Player> GetPlayers() {
        if (_players == null) {
            throw new System.Exception("PlayerInjector not ready");
        }
        return _players;
    }

    public PlayerContext GetPlayerOneContext() {
        return GetNode<PlayerContext>("/root/Main/Pausable/PlayerContext-1");
    }

    public Player GetPlayerOne() {
        return GetPlayerOneContext().Player;
    }

    private Player GetPlayerTwo() {
        if (PlayerManager.Ref().IsPlayerActive(PlayerID.Two)) {
            return GetNode<PlayerContext>("/root/Main/Pausable/PlayerContext-2").Player;
        }
        else {
            return GetNode<Player>("/root/Main/Pausable/InactivePlayer-2");
        }
    }
}
