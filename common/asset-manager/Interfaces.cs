public interface IGameAssetDTO {
    bool IsValid();
    string Stringify();
}

public interface IGameAssetDTOWithImages {
    string[] ImageAssetPaths();
}
