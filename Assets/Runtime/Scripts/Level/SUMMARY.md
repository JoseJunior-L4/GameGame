# Procedural Level Generator - Summary

## What You're Getting

A complete Unity procedural level generation system for 2D platformer battle royale games.

### 5 C# Scripts:

1. **ProceduralLevelGenerator.cs** - Editor window tool
2. **RuntimeLevelGenerator.cs** - Runtime generation component  
3. **WeaponSpawner.cs** - Weapon pickup system
4. **PlayerSpawnPointGenerator.cs** - Player spawn placement
5. **LevelGeneratorQuickSetup.cs** - One-click setup utility

## Key Features

✅ **Movement-Aware Generation**
- Takes player jump height and distance into account
- Ensures all platforms are reachable
- Perfect for movement shooters with lots of space

✅ **Dual Tilemap System**
- One tilemap for platforms (with collision)
- Separate tilemap for weapon spawn markers
- Clean separation of concerns

✅ **Battle Royale Optimized**
- Boundary walls to contain players
- Central platform for action hotspot
- Balanced weapon spawn distribution
- Multiple player spawn points spread out

✅ **Flexible Generation**
- Editor tool for design-time testing
- Runtime generation for dynamic levels
- Seed-based for reproducible results
- Highly configurable parameters

✅ **Complete Weapon System**
- Automatic weapon spawning at markers
- Pickup and respawn mechanics
- Distance-based spawn placement
- On-platform and in-air spawning

## Quick Start (3 Steps)

### 1. Setup
- Copy all `.cs` files to your Unity project
- Make sure 2D Tilemap Editor package is installed
- Create two tiles (platform and weapon marker)

### 2. Quick Setup Tool
- Go to `Tools > Quick Setup Level Generator`
- Assign your two tiles
- Click "Create Complete Setup"

### 3. Play!
- Press Play in Unity
- A level will generate automatically
- Adjust parameters in the Inspector as needed

## Perfect For

- 2D Battle Royale games
- Movement-focused platformer shooters
- Multiplayer arena games
- Procedural platformer content

## Customization

All parameters are exposed in the Inspector:
- Level dimensions (width/height)
- Platform density and size
- Weapon spawn counts and placement
- Player movement capabilities
- Arena features (walls, central platform)

## Advanced Usage

```csharp
// Get spawn positions for custom logic
List<Vector3> weaponPos = levelGen.GetWeaponSpawnWorldPositions();
List<Vector3> playerSpawns = spawnGen.GetSpawnPoints();

// Regenerate at runtime
levelGen.GenerateLevel();

// Get random spawn point
Vector3 spawn = spawnGen.GetRandomSpawnPoint();
```

## What Makes This Special

**Movement-Focused**: Unlike generic procedural generators, this one specifically considers player movement capabilities. Platforms are placed with jump height and distance in mind.

**Battle Royale Ready**: Includes spawn point distribution, weapon placement, boundary walls, and central gathering points - all the essentials for multiplayer battle royale.

**Tilemap Native**: Uses Unity's Tilemap system properly with collision, making it integrate seamlessly with your existing 2D game setup.

**Production Ready**: Includes weapon pickup/respawn, player spawn points, editor tools, and runtime generation - everything you need for a real game.

## Notes

- Designed for horizontal platformer movement (not wall-climbing games)
- Assumes standard platformer physics
- Test with your actual player jump values for best results
- Weapon markers are visual only - you need weapon prefabs
- Player must be tagged "Player" for weapon pickups

---

See README.md for complete documentation and examples!
