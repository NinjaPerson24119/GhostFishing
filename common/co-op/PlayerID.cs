public enum PlayerID {
    Invalid = -1,
    One = 1,
    Two = 2,
}

public static class PlayerIDExtensions {
    public static int PlayerNumber(this PlayerID playerID) {
        return (int)playerID;
    }

    public static int PlayerControlMappingNumber(this PlayerID playerID) {
        return (int)playerID - 1;
    }
}
