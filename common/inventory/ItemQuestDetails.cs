using System;

public class InventoryItemInstanceQuestDetailsDTO : IGameAssetDTO {
    public string? QuestID { get; set; }
    public string? UniqueID { get; set; }

    public bool Validate() {
        return !string.IsNullOrEmpty(QuestID) && !string.IsNullOrEmpty(UniqueID);
    }

    public string Stringify() {
        return $"QuestID: {QuestID}\nUniqueID: {UniqueID}";
    }
}

public class InventoryItemInstanceQuestDetails {
    public string QuestID { get; set; }
    public string UniqueID { get; set; }

    public InventoryItemInstanceQuestDetails(InventoryItemInstanceQuestDetailsDTO dto) {
        if (!dto.Validate()) {
            throw new ArgumentException("Invalid InventoryItemInstanceQuestDetailsDTO");
        }
        QuestID = dto.QuestID!;
        UniqueID = dto.UniqueID!;
    }
}
