# Water

Water waves are approximated with Gerstner Waves.

The water shader:
- applies a fresnel effect
- applies a normal map to the water surface, generated from noise
- displaces the water surface with a sum of Gerstner Waves

The `Ocean` component:
- Holds parameters for the global water surface
- Generates tiles of `WaterTile` as plane meshes
- Handles LOD for the water tiles, replacing them with a lower resolution mesh when they are far away

The `WaterTile` component:
- Configures the water shader on a per tile basis
- Contains a `WaveSet` component, which holds parameters for the Gerstner Waves

The `WaveSet` component:
- Generates parameters for Gerstner Waves based on some high-level inputs such as wind direction
- Allows for slowly changing the wave parameters over time, to simulate changing weather conditions
  - First, reconfigure the `WaveSet`
  - Then iteratively sample new waves
