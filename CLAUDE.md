# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Godot 4.6 game project ("Cars") using C# (.NET 8.0) with the Godot.NET.Sdk. The main scene is a spinning cube on a checkered ground plane, controllable with arrow keys.

## Build & Test Commands

```bash
# Build the Godot project
dotnet build cars/Cars.csproj

# Build the full solution (game + tests)
dotnet build cars/Cars.sln

# Run all tests
dotnet test tests/Tests/Tests.csproj

# Run a single test by name
dotnet test tests/Tests/Tests.csproj --filter "FullyQualifiedName~Tests.CubeRotationLogicTests.LeftKey_RotatesPositiveY"
```

## Architecture

**Two-project solution** (`cars/Cars.sln`):
- `cars/Cars.csproj` — Godot game project (Godot.NET.Sdk 4.6.0, targets net8.0)
- `tests/Tests/Tests.csproj` — xUnit test project (net8.0), references Cars.csproj

**Key pattern: Godot-independent logic separation.** Game logic is extracted into plain C# classes (no Godot dependency) so it can be unit tested outside the engine:
- `cars/CubeRotationLogic.cs` — Pure C# rotation computation (testable)
- `cars/SpinningCube.cs` — Godot node script that delegates to `CubeRotationLogic` for actual math, handles only engine I/O (input reading, applying transforms)

New gameplay logic should follow this same pattern: keep computation in plain C# classes, wrap them in Godot node scripts.

## Godot Configuration

- Engine: Godot 4.6, C# / .NET
- Physics: Jolt Physics 3D
- Renderer: Forward Plus, D3D12 on Windows
- Main scene: `cars/world.tscn`
- Export target: Windows x86_64 (`cargame.exe`)
