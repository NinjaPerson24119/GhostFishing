# Asset Manager

1. Loads assets from the file system into DTOs
2. Passed the DTOs to `AssetStore` instances which runs validation and checks dependencies
3. Provides access `AssetStore` instances

## About temporary inventories

Just make an `Inventory`. Because it isn't tracked, it'll be deleted when the last reference to it is gone.
