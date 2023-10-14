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

We apply drag forces using the drag coefficients in water and air. These are weighted by how submerged the boat is.
This works generally well, but can make the boat feel "floaty".
I could probably solve this by fine tuning the drag coefficients and the weight of the boat.

Instead, I apply a `ConstantDrag` that just reduces the velocity by a constant factor each frame.
This helps smooth out the movement, and makes the boat feel heavier.
I don't really want realistic physics when in midair, for example, so this is a good compromise to dampen excessive force.

### Buoyancy

Archimedes Principle is applied at Marker3D points distributed in a + shape around the boat. The boat is pushed up by the sum of the buoyancy forces at each point.

The weight and dimensions of the boat must be physically accurate for this to work.

### Drag Coefficients

Ask ChatGPT:
```
write me tables for the linear and angular drag coefficients in both air and water, for a few kinds of boats
```

Boat Variety	Linear Drag Coefficient (Cd)	Angular Drag Coefficient (Cm)
High-Performance Racing Boat	0.02 - 0.05	0.02 - 0.05
Recreational Powerboat	0.25 - 0.45	0.02 - 0.06
Sailboat (varies with design)	0.20 - 0.35	0.01 - 0.04
Fishing Boat	0.30 - 0.50	0.03 - 0.08
Kayak (varies with design)	0.20 - 0.40	0.01 - 0.03
Linear and Angular Drag Coefficients for Boats in Water:

Boat Variety	Linear Drag Coefficient (Cd)	Angular Drag Coefficient (Cm)
High-Performance Racing Boat	0.005 - 0.02	0.02 - 0.05
Recreational Powerboat	0.1 - 0.3	0.02 - 0.06
Sailboat (varies with design)	0.05 - 0.15	0.01 - 0.03
Fishing Boat	0.15 - 0.30	0.03 - 0.08
Kayak (varies with design)	0.1 - 0.25	0.01 - 0.02

### Boat Parameters

- Row Boat
  - 150kg, (1, 0.5, 2.5)m
  - 0.5 submersion factor
- Trawler
  - 2500kg, (2, 3, 2)m
  - 0.7 submersion factor
