public class QuestDTO : IGameAssetDTO {
    public string? Name { get; set; }
    public string? Description { get; set; }

    public bool Validate() {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Description);
    }

    public string Stringify() {
        return $"Quest: {Name}\nDescription: {Description}";
    }
}

public class Quest {
    string Name { get; set; }
    string Description { get; set; }

    public Quest(QuestDTO dto) {
        Name = dto.Name ?? "";
        Description = dto.Description ?? "";
    }
}
