using System;

public class FishDefinitionDTO : InventoryItemDefinitionDTO, IGameAssetDTO {
    public override bool Validate() {
        return base.Validate();
    }

    public override string Stringify() {
        return base.Stringify();
    }
}

public class FishDefinition : InventoryItemDefinition {
    public FishDefinition(FishDefinitionDTO dto) : base(dto) {
        if (!dto.Validate()) {
            throw new ArgumentException("Invalid FishDefinitionDTO");
        }
    }

    // TODO: Need to pre-load images for fish
    /*
    public void Load() {

    }
    */
}
