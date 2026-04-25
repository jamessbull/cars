# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Godot 4.6 car racing game ("Cars") using C# (.NET 8.0) with the Godot.NET.Sdk. The game features a car with spring suspension physics, a tile-based procedural track system, and an in-engine map editor. The main gameplay scene is `cars/world.tscn`; the map editor runs from `cars/editor.tscn`.

## Build & Test Commands

```bash
# Build the Godot project
dotnet build cars/Cars.csproj

# Build the full solution (game + tests)
dotnet build cars/Cars.sln

# Run all tests
dotnet test tests/Tests/Tests.csproj

# Run a single test by name
dotnet test tests/Tests/Tests.csproj --filter "FullyQualifiedName~Tests.CarPhysicsTests.Throttle_AppliesForwardForce"
```

## Architecture

**Two-project solution** (`cars/Cars.sln`):
- `cars/Cars.csproj` — Godot game project (Godot.NET.Sdk 4.6.0, targets net8.0)
- `tests/Tests/Tests.csproj` — xUnit test project (net8.0), references Cars.csproj

### Core pattern: Godot-independent logic separation

All game logic is extracted into plain C# classes with no Godot dependency so it can be unit tested outside the engine. Godot node scripts handle only engine I/O (input reading, raycasts, applying forces/transforms). Every new system should follow this same split.

**Physics pipeline** (called each `_PhysicsProcess` from `CarBody.cs`):
- `CarPhysicsTypes.cs` — data-only structs: `CarInput`, `WheelRayData`, `WheelResult`, `CarPhysicsResult`
- `CarConstants.cs` — all tuning values (suspension, drive forces, steering, camera)
- `SuspensionSpring.cs` — spring/damper force from a `SpringInput` (hit distance + vertical velocity)
- `SteeringModel.cs` — bicycle-model steering: ramps angle toward input, derives yaw rate from speed/geometry
- `CarPhysicsLogic.cs` — orchestrates suspension + steering + drive force for one frame; returns `CarPhysicsResult`
- `CarBody.cs` (Godot node) — fires raycasts for 4 wheel positions, feeds into `CarPhysicsLogic`, applies forces/velocities, positions wheel meshes

**Track system:**
- `TileGeometry.cs` — defines `TileType` enum, `TileMeshData` struct, and pure C# mesh generation for all tile shapes (Flat, Ramp, RampEntry, RampExit, Fence, Gravel, SlopeEdge, SlopeCorner, RampEntryCorner, SolidRampEntry). Cell dimensions and arc geometry are computed from a single `ExitAngleRad` constant so ramp/arc tile heights are integer multiples of `CellHeight`.
- `TileMeshBuilder.cs` (Godot dependency) — converts `TileMeshData` into Godot `ArrayMesh`/`MeshLibrary` and defines per-tile materials
- `TrackLayout.cs` — `TilePlacement` struct, `CardinalDirection` enum, `TrackLayout.GetOrientationIndex()` (maps facing→Godot GridMap orientation index), and `GetDemoTrack()` (hardcoded test layout)
- `TrackLayoutLoader.cs` — pure C# JSON serializer/deserializer for `track_layout.json` (uses `System.Text.Json`)
- `TrackGridMap.cs` (Godot node) — loads `track_layout.json` (falling back to demo track), populates a `GridMap`, spawns the car at grid (0,0,0)

**Map editor:**
- `MapEditorState.cs` — pure C# editor state: selected tile type, facing, grid Y level, placed tile dictionary, JSON serialization
- `MapEditorNode.cs` (Godot node) — builds the full editor scene in `_Ready()` (no .tscn child nodes); handles mouse input for tile placement/deletion, scroll wheel for height/type/facing, Save button writes `track_layout.json`

### Track layout JSON format

```json
{
  "TrackLayout": [
    { "type": "Flat", "x": 0, "y": 0, "z": 0, "facing": "North" },
    { "type": "RampEntry", "x": 1, "y": 0, "z": 0, "facing": "East" }
  ]
}
```
`track_layout.json` lives at `cars/track_layout.json` (Godot `res://track_layout.json`). Type and facing values are matched case-insensitively.

## Godot Configuration

- Engine: Godot 4.6, C# / .NET
- Physics: Jolt Physics 3D
- Renderer: Forward Plus, D3D12 on Windows
- Main scene: `cars/world.tscn`
- Editor scene: `cars/editor.tscn`
- Export target: Windows x86_64 (`cargame.exe`)
