## Visual vs Physical

RigidBody3D is used for physics so that we can apply forces to the player for movement.
- CharacterBody3D results in very unnatural motion due to lack of inertia.

However, we want the orientation of the boat to be independent of the physics.
We want the boat to shift around to match the motion of the waves, but this shouldn't affect controls.

So we use a separate Visual node to handle the orientation of the boat.
Physics apply to a neutrally oriented RigidBody3D.

## Buoyancy

We don't want the boat to capsize, or deal with complex buoyancy physics.
So we fix the boat's Y coordinate to the water's height.
We then fix linear velocity Y to be 0, and disable gravity.
