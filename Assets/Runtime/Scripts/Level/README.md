# Procedural Level Generator for 2D Platformer Battle Royale

A complete Unity editor tool and runtime system for generating procedural 2D platformer levels with weapon spawn points.

## Features

- **Tilemap-Based Generation**: Uses Unity's Tilemap system for platforms and weapon spawns
- **Player Movement Aware**: Takes into account player jump height and distance for playable layouts
- **Battle Royale Optimized**: Designed for movement shooter gameplay with ample space
- **Connectivity System**: Ensures all platforms are reachable by the player
- **Flexible Weapon Spawning**: Spawn weapons on platforms or in the air
- **Editor Tool**: Generate levels in-editor with full parameter control
- **Runtime Generation**: Generate levels at game start or on-demand
- **Automatic Weapon Spawning**: Complete weapon pickup and respawn system

---

## Installation

1. **Copy Scripts to Unity Project**
   - Place all `.cs` files in your `Assets/Scripts/` folder (or any folder in your Assets)
   - Unity will automatically compile them

2. **Required Unity Packages**
   - Ensure you have the **2D Tilemap Editor** package installed
   - Go to `Window > Package Manager > Unity Registry > 2D Tilemap Editor`

---

## Quick Start Guide

### Part 1: Setting Up Tilemaps

1. **Create Grid GameObject**
   - Right-click in Hierarchy
   - `2D Object > Tilemap > Rectangular`
   - This creates a Grid with a Tilemap child

2. **Create Second Tilemap for Weapons**
   - Right-click on the Grid object
   - `2D Object > Tilemap > Rectangular`
   - Rename the two tilemaps:
     - First one: `PlatformTilemap`
     - Second one: `WeaponSpawnTilemap`

3. **Create Tiles**
   - Create a folder: `Assets/Tiles/`
   - Right-click in the folder
   - `Create > 2D > Tiles > Tile`
   - Create two tiles:
     - `PlatformTile` - Assign a platform sprite
     - `WeaponSpawnTile` - Assign a marker sprite (like a star or crosshair)

4. **Adjust Tilemap Rendering**
   - Select `WeaponSpawnTilemap`
   - In the Tilemap Renderer component:
     - Change `Sorting Layer` or `Order in Layer` so weapon markers appear above platforms

---

### Part 2: Using the Editor Tool

1. **Open the Level Generator**
   - Go to `Tools > Procedural Level Generator`
   - A window will appear

2. **Assign References**
   - **Platform Tilemap**: Drag `PlatformTilemap` from the Hierarchy
   - **Weapon Tilemap**: Drag `WeaponSpawnTilemap` from the Hierarchy
   - **Platform Tile**: Drag `PlatformTile` from your Tiles folder
   - **Weapon Spawn Tile**: Drag `WeaponSpawnTile` from your Tiles folder

3. **Adjust Parameters**
   - **Level Width/Height**: Size of your arena (default: 100x60)
   - **Jump Height/Distance**: Match your player's movement capabilities
   - **Platform Density**: How many platforms to generate (35% is good for movement shooters)
   - **Weapon Spawn Count**: How many weapon pickups (20 is default)

4. **Generate!**
   - Click `Generate Level`
   - Your level will appear in the Scene view
   - Click `Clear Level` to remove it and try different parameters

5. **Save Your Favorite Settings**
   - You can try different seeds or use random seeds
   - Take note of seeds you like for reproducible levels

---

### Part 3: Runtime Generation

1. **Create Level Manager GameObject**
   - Right-click in Hierarchy
   - `Create Empty`
   - Rename to `LevelManager`

2. **Add Runtime Generator**
   - Select `LevelManager`
   - `Add Component > Runtime Level Generator`

3. **Assign References**
   - Drag your tilemaps and tiles (same as editor tool)
   - Configure generation parameters
   - Check `Generate On Start` if you want automatic generation

4. **Add Weapon Spawner (Optional)**
   - Select `LevelManager`
   - `Add Component > Weapon Spawner`
   - Assign the `RuntimeLevelGenerator` component
   - Create weapon prefabs and assign them to `Weapon Prefabs` array

---

## Creating Weapon Prefabs

1. **Create a Weapon GameObject**
   - Right-click in Hierarchy > `2D Object > Sprite`
   - Assign a weapon sprite
   - Add a `Box Collider 2D` component
   - Set `Is Trigger` to true

2. **Tag Setup**
   - Make sure your player has the tag `Player`
   - Go to `Edit > Project Settings > Tags and Layers`
   - Add "Player" tag if it doesn't exist

3. **Save as Prefab**
   - Drag the weapon from Hierarchy to a `Prefabs` folder
   - Delete from scene

4. **Repeat for Multiple Weapon Types**
   - Create different weapon prefabs (rifle, shotgun, sniper, etc.)
   - Assign them all to the Weapon Spawner's array

---

## Parameter Guide

### Player Movement Parameters

- **Jump Height**: Vertical tiles the player can jump (test in your game)
- **Jump Distance**: Horizontal tiles the player can jump
- **Min Platform Spacing**: Minimum vertical gap between platforms

### Platform Generation

- **Min/Max Platform Length**: Size variety for platforms
- **Platform Density**: Percentage of level filled with platforms
  - 20-30%: Sparse, lots of jumping
  - 35-45%: Balanced for movement shooters
  - 50-70%: Dense, more cover
- **Ensure Connectivity**: Adds bridge platforms to connect gaps
- **Height Variation**: How much platforms vary in height

### Weapon Spawn Settings

- **Weapon Spawn Count**: Total number of weapon pickups
- **Min Weapon Distance**: Minimum space between weapons (prevents clustering)
- **Spawn On Platforms**: Place weapons on platform surfaces
- **Spawn In Air**: Place weapons floating above platforms
- **Air Spawn Max Height**: How high above platforms weapons can float

### Arena Design

- **Create Boundary Walls**: Adds walls around the arena perimeter
- **Boundary Wall Height**: Height of perimeter walls
- **Create Central Platform**: Adds a large platform in the center
- **Central Platform Size**: Width of the central platform

---

## Advanced Usage

### Getting Spawn Positions Programmatically

```csharp
// Get reference to level generator
RuntimeLevelGenerator levelGen = GetComponent<RuntimeLevelGenerator>();

// Get all weapon spawn positions
List<Vector3> weaponPositions = levelGen.GetWeaponSpawnWorldPositions();

// Use them for custom logic
foreach (Vector3 pos in weaponPositions)
{
    Debug.Log($"Weapon spawn at: {pos}");
    // Spawn your own objects here
}
```

### Getting Platform Data

```csharp
// Get platform information
List<RuntimeLevelGenerator.PlatformData> platforms = levelGen.GetPlatforms();

// Use for AI navigation, spawn point selection, etc.
foreach (var platform in platforms)
{
    Vector3Int center = platform.GetCenterPosition();
    Debug.Log($"Platform at {center} with length {platform.length}");
}
```

### Regenerating Levels

```csharp
// Regenerate with new random seed
levelGen.useRandomSeed = true;
levelGen.GenerateLevel();

// Regenerate with specific seed
levelGen.useRandomSeed = false;
levelGen.seed = 12345;
levelGen.GenerateLevel();
```

### Custom Weapon Pickup Logic

Modify the `WeaponPickup.PickupWeapon()` method to integrate with your inventory system:

```csharp
private void PickupWeapon(GameObject player)
{
    isPickedUp = true;
    
    // YOUR CUSTOM CODE HERE
    PlayerInventory inventory = player.GetComponent<PlayerInventory>();
    if (inventory != null)
    {
        inventory.AddWeapon(weaponName);
    }
    
    // Rest of the pickup code...
}
```

---

## Tips for Battle Royale Levels

1. **Use Larger Dimensions**: 100x60 or larger for battle royale
2. **Medium Density**: 30-40% platform density gives room to move
3. **Enable Connectivity**: Essential for fair gameplay
4. **Boundary Walls**: Keeps players in the arena
5. **Central Platform**: Creates a hotspot for action
6. **Spread Weapons**: Use min distance to prevent weapon clusters
7. **Mix Air and Ground Spawns**: Rewards vertical movement

### Recommended Settings for Movement Shooters

```
Level Width: 120
Level Height: 70
Player Jump Height: 5
Player Jump Distance: 8
Platform Density: 35%
Min Platform Length: 4
Max Platform Length: 12
Weapon Spawn Count: 25
Min Weapon Distance: 12
Create Central Platform: Yes
Ensure Connectivity: Yes
```

---

## Customization Ideas

1. **Multiple Levels**: Store seeds for favorite layouts
2. **Progressive Difficulty**: Increase platform spacing over time
3. **Special Platforms**: Modify platform types with special properties
4. **Hazards**: Add spike or lava tiles using the same tilemap system
5. **Power-ups**: Use the weapon spawner system for power-ups too
6. **Shrinking Zone**: Battle royale zone that gets smaller over time
7. **Dynamic Regeneration**: Regenerate sections mid-game

---

## Troubleshooting

**Platforms don't appear:**
- Check that tilemap and tile are assigned
- Verify the tile has a sprite assigned
- Check the camera can see the tilemaps

**Weapon spawns overlap:**
- Increase `Min Weapon Distance`
- Reduce `Weapon Spawn Count`

**Platforms aren't connected:**
- Enable `Ensure Connectivity`
- Reduce `Max Height Variation`
- Increase `Player Jump Distance`

**Level is too sparse/dense:**
- Adjust `Platform Density` percentage
- Modify `Min/Max Platform Length`

**Weapons don't spawn:**
- Ensure weapon prefabs have colliders
- Check that player has "Player" tag
- Verify `WeaponSpawner` reference is set

---

## Script Overview

### ProceduralLevelGenerator.cs
- Editor window tool for designing levels in the Unity editor
- Full parameter control with immediate visual feedback
- Save/load seeds for reproducible levels

### RuntimeLevelGenerator.cs
- Runtime component for generating levels during gameplay
- Can be called on Start or on-demand
- Provides access to platform and weapon spawn data

### WeaponSpawner.cs
- Spawns weapon prefabs at generated positions
- Handles weapon pickups and respawning
- Customizable pickup logic

---

## License & Credits

Created for 2D Platformer Battle Royale projects.
Feel free to modify and extend for your game!

For questions or improvements, modify the scripts to fit your needs.
