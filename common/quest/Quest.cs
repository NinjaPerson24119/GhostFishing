using System;

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
    public string Name { get; set; }
    public string Description { get; set; }

    public Quest(QuestDTO dto) {
        if (!dto.Validate()) {
            throw new ArgumentException("Invalid QuestDTO");
        }
        Name = dto.Name!;
        Description = dto.Description!;
    }
}
