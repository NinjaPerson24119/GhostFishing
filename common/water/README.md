# Water

Water waves are approximated with Gerstner Waves.

The water shader:
- applies a fresnel effect
- displaces the water surface with a sum of Gerstner Waves
- applies height noise in the direction of the wave normals
- applies a normal map to the water surface

The `Ocean` component:
- Holds global `WaveSet` for the global water surface
- Generates tiles of `WaterTile` as plane meshes
- Handles LOD for the water tiles, replacing them with a lower resolution mesh when they are far away
- Centers the `Ocean` on the player ("origin")

The `WaterTile` component:
- Configures the water shader on a per tile basis
- Contains a `WaveSet` reference, which holds parameters for the Gerstner Waves
- The `WaterTile` can be instantiated without an `Ocean`, in which case it will have its own `WaveSet`

The `WaveSet` component:
- Generates parameters for Gerstner Waves based on some high-level inputs such as wind direction
- Allows for slowly changing the wave parameters over time, to simulate changing weather conditions
  - First, reconfigure the `WaveSet`
  - Then iteratively sample new waves
