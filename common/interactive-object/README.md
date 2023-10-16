# Interactive Object

An interactive object is one that prompts for a user interaction.
E.g. "Press A to open chest" or "Press A to talk to NPC".

## Publisher Pattern

Querying for object positions every frame is really expensive.
Instead, each interactive object publishes its position to a tracking server.

Objects can then subscribe to the tracking server to get the positions of all interactive objects.

## Tracker

Each interactive object has a tracker that publishes its position to the tracking server if it has changed significantly by some epsilon.

## Tracker Server

Global coordinates are binned into a grid of tiles.

## Subscriber

A subscriber can query interactive objects by their ID or by the tile they are in.
This allows them to select for only nearby objects.
