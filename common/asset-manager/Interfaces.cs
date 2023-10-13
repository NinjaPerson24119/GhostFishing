public interface IGameAssetDTO {
    bool IsValid();
    string Stringify();
}

public interface IGameAssetDTOWithImages {
    string[] ImageAssetPaths();
}

public interface IGameAssetWritable<DTO> where DTO : IGameAssetDTO {
    DTO ToDTO();
    bool IsTouched();
}
