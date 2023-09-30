# Save State

## Format

Saves are stored in JSON.
- Don't want to tightly couple to Godot Resource format
  - And want to be able to use external editors
- C# doesn't have a standard library for YAML, and I hate hand editing YAML

## Hierarchy

To be agnostic to the number of players the save state holds an array of player states.
Much state is stored between players, such as the time of day, money, and the state of the world.
