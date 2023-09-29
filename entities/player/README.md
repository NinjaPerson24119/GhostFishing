# Player Physics

## What doesn't work
- CharacterBody3D
  - Doesn't provide inertia, making movement feel mechanical
- RigidBody3D with fixed Y to water height
  - Makes it impossible to capsize the boat, or get air. This makes huge waves feel less dramatic.
- RigidBody3D with computed pitch and yaw
  - Aligning the boat with the waves results in jerky movement. This can be alleviated with a running average smoothing, but it's a losing battle.
  - Even with the smoothing, the number of samples greatly affects the fitted plane when the waves are small.

## Full RigidBody3D

This approach gives all the physics we want, for the least effort.
- Capsizing
- Getting air
- Inertia
- Wobbling the boat to wave fluctuations
- Beaching the boat
- Adjusting engine strength to the level of submersion

### Solving acceleration with drag forces

However, this makes the boat extremely difficult to control.
The accumulated force sort of just tosses the boat around.

This is solved through `ConstantDrag` and `WaterDrag`.

We apply `ConstantDrag` always. This prevents the boat from spinning crazily when it's in the air. It also makes the boat feel heavier, which is good.

We apply `WaterDrag` when the boat is in the water. This drag is on top of the `ConstantDrag`. It is proportional to how submerged the boat is.
This means it's harder to move the boat when it's underwater, and easier when it's on the surface.

### Buoyancy

Archimedes Principle is applied at Marker3D points distributed in a + shape around the boat. The boat is pushed up by the sum of the buoyancy forces at each point.
