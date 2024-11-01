using System;

internal class QuestDefinitionDTO : IGameAssetDTO {
    public string? Name { get; set; }
    public string? Description { get; set; }

    public bool IsValid() {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Description);
    }

    public string Stringify() {
        return $"Quest: {Name}\nDescription: {Description}";
    }
}

internal class QuestDefinition {
    public string Name { get; set; }
    public string Description { get; set; }

    public QuestDefinition(QuestDefinitionDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid QuestDefinitionDTO");
        }
        Name = dto.Name!;
        Description = dto.Description!;
    }
}
