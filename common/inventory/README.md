# Inventory

## Placement

Shapes are all stored as rectangular grids:
- width, height for dimensions
- mask to indicate which cells are filled

## Item Transport

Contains an `Item` while it is being transported from one `Inventory` to another.
It's basically a clipboard that follows the mouse / selected space.
