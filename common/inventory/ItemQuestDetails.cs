using System;

public class InventoryItemInstanceQuestDetailsDTO : IGameAssetDTO {
    public string? QuestDefinitionID { get; set; }
    public string? UniqueID { get; set; }

    public bool IsValid() {
        return !string.IsNullOrEmpty(QuestDefinitionID) && !string.IsNullOrEmpty(UniqueID);
    }

    public string Stringify() {
        return $"QuestDefinitionID: {QuestDefinitionID}\nUniqueID: {UniqueID}";
    }
}

public class InventoryItemInstanceQuestDetails {
    public string QuestDefinitionID { get; set; }
    public string UniqueID { get; set; }

    public InventoryItemInstanceQuestDetails(InventoryItemInstanceQuestDetailsDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryItemInstanceQuestDetailsDTO");
        }
        QuestDefinitionID = dto.QuestDefinitionID!;
        UniqueID = dto.UniqueID!;
    }
}
