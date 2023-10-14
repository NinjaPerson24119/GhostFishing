using System;

internal class FishDefinitionDTO : InventoryItemDefinitionDTO, IGameAssetDTO {
    public override bool IsValid() {
        return base.IsValid();
    }

    public override string Stringify() {
        return base.Stringify();
    }
}

internal class FishDefinition : InventoryItemDefinition {
    public FishDefinition(FishDefinitionDTO dto) : base(dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid FishDefinitionDTO");
        }
    }

    // TODO: Need to pre-load images for fish
    /*
    public void Load() {

    }
    */
}
