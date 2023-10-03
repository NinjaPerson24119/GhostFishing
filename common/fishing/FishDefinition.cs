public class FishDefinitionDTO : InventoryItemDefinitionDTO, IGameAssetDTO {
    public override bool Validate() {
        return base.Validate();
    }

    public override string Stringify() {
        return base.Stringify();
    }
}

public class FishDefinition : InventoryItemDefinition {
    // TODO: Need to pre-load images for fish
    /*
    public void Load() {

    }
    */
}
