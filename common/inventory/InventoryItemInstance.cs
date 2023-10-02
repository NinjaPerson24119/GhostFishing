public class InventoryItemInstanceQuestDetails : IValidatedGameAsset {
    public string QuestID;
    public string UniqueID;
    public bool Validate() {
        return QuestID.Length == 0 || UniqueID.Length == 0;
    }

    public string Stringify() {
        return $"QuestID: {QuestID}\nUniqueID: {UniqueID}";
    }
}

public class InventoryItemInstance : IValidatedGameAsset {
    public string DefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public InventoryItemInstanceQuestDetails QuestDetails { get; set; }
    public InventoryItemFlags FlagOverrides { get; set; }

    public bool Validate() {
        if (DefinitionID.Length == 0) {
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
