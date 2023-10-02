public class FishDefinition : InventoryItemDefinition, IValidatedGameAsset {
    // TODO: Need to pre-load images for fish
    /*
    public void Load() {

    }
    */

    public override bool Validate() {
        return base.Validate();
    }

    public override string Stringify() {
        return base.Stringify();
    }
}
