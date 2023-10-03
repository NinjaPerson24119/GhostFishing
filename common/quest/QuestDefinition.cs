using System;

public class QuestDefinitionDTO : IGameAssetDTO {
    public string? Name { get; set; }
    public string? Description { get; set; }

    public bool Validate() {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Description);
    }

    public string Stringify() {
        return $"Quest: {Name}\nDescription: {Description}";
    }
}

public class QuestDefinition {
    public string Name { get; set; }
    public string Description { get; set; }

    public QuestDefinition(QuestDefinitionDTO dto) {
        if (!dto.Validate()) {
            throw new ArgumentException("Invalid QuestDefinitionDTO");
        }
        Name = dto.Name!;
        Description = dto.Description!;
    }
}
