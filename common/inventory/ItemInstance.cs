public class InventoryItemInstanceDTO : IGameAssetDTO {
    public string? DefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public InventoryItemInstanceQuestDetailsDTO? QuestDetails { get; set; }
    public InventoryItemFlagsDTO? FlagOverrides { get; set; }

    public bool Validate() {
        if (string.IsNullOrEmpty(DefinitionID)) {
            return false;
        }
        if (X < 0 || Y < 0) {
            return false;
        }
        if (QuestDetails != null) {
            if (!QuestDetails.Validate()) {
                return false;
            }
        }
        return true;
    }

    public string Stringify() {
        string str = $"DefinitionID: {DefinitionID}\n";
        str += $"X: {X}\nY: {Y}\nRotation: {Rotation}\n";
        if (QuestDetails != null) {
            str += $"QuestDetails:\n{QuestDetails.Stringify()}\n";
        }
        if (FlagOverrides != null) {
            str += $"FlagOverrides:\n{FlagOverrides.Stringify()}\n";
        }
        return str;
    }
}

public class InventoryItemInstance {
    public string DefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public InventoryItemInstanceQuestDetails QuestDetails { get; set; }
    public InventoryItemFlags FlagOverrides { get; set; }
}
