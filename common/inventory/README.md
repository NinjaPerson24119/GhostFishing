# Inventory

## Nullable spatial

`InventoryItem` deliberately encapsulates the `InventoryItemSpatial` since we may want to
store items in the inventory that don't have a spatial representation.

## Placement

Shapes are all stored as rectangular grids:
- width, height for dimensions
- mask to indicate which cells are filled
