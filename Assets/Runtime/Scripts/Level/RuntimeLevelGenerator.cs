using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Runtime Procedural Level Generator
/// Can be called at game start or on-demand to generate levels
/// </summary>
public class RuntimeLevelGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap platformTilemap;
    public Tilemap weaponSpawnTilemap;
    public TileBase platformTile;
    public TileBase weaponSpawnTile;
    
    [Header("Level Dimensions")]
    public int levelWidth = 100;
    public int levelHeight = 60;
    
    [Header("Player Movement Parameters")]
    public int playerJumpHeight = 5;
    public int playerJumpDistance = 8;
    public int minPlatformSpacing = 2;
    
    [Header("Platform Generation")]
    public int minPlatformLength = 3;
    public int maxPlatformLength = 15;
    [Range(10, 70)] public int platformDensity = 35;
    public bool ensureConnectivity = true;
    public int minHeightVariation = 3;
    public int maxHeightVariation = 8;
    
    [Header("Weapon Spawn Settings")]
    public int weaponSpawnCount = 20;
    public int minWeaponDistance = 10;
    public bool spawnOnPlatforms = true;
    public bool spawnInAir = true;
    public int airSpawnMaxHeight = 3;
    
    [Header("Arena Design")]
    public bool createBoundaryWalls = true;
    public int boundaryWallHeight = 10;
    public bool createCentralPlatform = true;
    public int centralPlatformSize = 12;
    
    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public int seed = 0;
    public bool useRandomSeed = true;
    
    private System.Random random;
    private List<PlatformData> platforms = new List<PlatformData>();
    private List<Vector3Int> weaponSpawnPositions = new List<Vector3Int>();
    
    private void Start()
    {
        if (generateOnStart)
        {
            GenerateLevel();
        }
    }
    
    /// <summary>
    /// Main level generation method - call this to generate a new level
    /// </summary>
    public void GenerateLevel()
    {
        if (useRandomSeed)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        random = new System.Random(seed);
        
        Debug.Log($"[Runtime Generator] Generating level with seed: {seed}");
        
        ClearLevel();
        platforms.Clear();
        weaponSpawnPositions.Clear();
        
        GenerateBoundaries();
        GenerateCentralPlatform();
        GeneratePlatforms();
        
        if (ensureConnectivity)
        {
            EnsurePlatformConnectivity();
        }
        
        DrawPlatforms();
        GenerateWeaponSpawns();
        
        Debug.Log($"[Runtime Generator] Complete! {platforms.Count} platforms, {weaponSpawnPositions.Count} weapon spawns");
    }
    
    /// <summary>
    /// Get all weapon spawn positions (useful for spawning actual weapon pickups)
    /// </summary>
    public List<Vector3> GetWeaponSpawnWorldPositions()
    {
        List<Vector3> worldPositions = new List<Vector3>();
        foreach (var tilePos in weaponSpawnPositions)
        {
            worldPositions.Add(weaponSpawnTilemap.CellToWorld(tilePos) + weaponSpawnTilemap.tileAnchor);
        }
        return worldPositions;
    }
    
    /// <summary>
    /// Get platform data (useful for AI navigation or spawn point selection)
    /// </summary>
    public List<PlatformData> GetPlatforms()
    {
        return new List<PlatformData>(platforms);
    }
    
    /// <summary>
    /// Clear the level
    /// </summary>
    public void ClearLevel()
    {
        if (platformTilemap != null)
        {
            platformTilemap.ClearAllTiles();
        }
        
        if (weaponSpawnTilemap != null)
        {
            weaponSpawnTilemap.ClearAllTiles();
        }
    }
    
    private void GenerateBoundaries()
    {
        if (!createBoundaryWalls) return;
        
        platforms.Add(new PlatformData(0, 0, levelWidth, PlatformType.Boundary));
        
        for (int y = 0; y <= boundaryWallHeight; y++)
        {
            platforms.Add(new PlatformData(0, y, 1, PlatformType.Boundary));
        }
        
        for (int y = 0; y <= boundaryWallHeight; y++)
        {
            platforms.Add(new PlatformData(levelWidth - 1, y, 1, PlatformType.Boundary));
        }
    }
    
    private void GenerateCentralPlatform()
    {
        if (!createCentralPlatform) return;
        
        int centerX = levelWidth / 2 - centralPlatformSize / 2;
        int centerY = levelHeight / 3;
        
        platforms.Add(new PlatformData(centerX, centerY, centralPlatformSize, PlatformType.Central));
    }
    
    private void GeneratePlatforms()
    {
        int targetTiles = (levelWidth * levelHeight * platformDensity) / 100;
        int currentTiles = 0;
        int attempts = 0;
        int maxAttempts = 1000;
        
        int currentHeight = 5;
        
        while (currentTiles < targetTiles && attempts < maxAttempts)
        {
            attempts++;
            
            int platformLength = random.Next(minPlatformLength, maxPlatformLength + 1);
            int platformX = random.Next(5, levelWidth - platformLength - 5);
            
            int heightChange = random.Next(-maxHeightVariation, maxHeightVariation + 1);
            currentHeight = Mathf.Clamp(currentHeight + heightChange, 
                                       playerJumpHeight + 2, 
                                       levelHeight - 5);
            
            if (IsPlatformValid(platformX, currentHeight, platformLength))
            {
                platforms.Add(new PlatformData(platformX, currentHeight, platformLength, PlatformType.Normal));
                currentTiles += platformLength;
                
                if (random.Next(0, 100) < 20)
                {
                    currentHeight = random.Next(playerJumpHeight + 2, levelHeight / 2);
                }
            }
        }
    }
    
    private bool IsPlatformValid(int x, int y, int length)
    {
        foreach (var platform in platforms)
        {
            if (platform.type == PlatformType.Boundary) continue;
            
            bool horizontalOverlap = !(x + length < platform.x || x > platform.x + platform.length);
            bool verticalClose = Mathf.Abs(y - platform.y) < minPlatformSpacing;
            
            if (horizontalOverlap && verticalClose)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void EnsurePlatformConnectivity()
    {
        platforms.Sort((a, b) => a.x.CompareTo(b.x));
        
        for (int i = 0; i < platforms.Count - 1; i++)
        {
            var current = platforms[i];
            var next = platforms[i + 1];
            
            if (current.type == PlatformType.Boundary || next.type == PlatformType.Boundary) continue;
            
            float horizontalDist = next.x - (current.x + current.length);
            float verticalDist = Mathf.Abs(next.y - current.y);
            
            if (horizontalDist > playerJumpDistance || verticalDist > playerJumpHeight)
            {
                int bridgeX = current.x + current.length + (int)(horizontalDist / 2);
                int bridgeY = (current.y + next.y) / 2;
                int bridgeLength = random.Next(minPlatformLength, minPlatformLength + 3);
                
                if (IsPlatformValid(bridgeX, bridgeY, bridgeLength))
                {
                    platforms.Add(new PlatformData(bridgeX, bridgeY, bridgeLength, PlatformType.Bridge));
                }
            }
        }
    }
    
    private void DrawPlatforms()
    {
        foreach (var platform in platforms)
        {
            for (int x = 0; x < platform.length; x++)
            {
                Vector3Int tilePos = new Vector3Int(platform.x + x, platform.y, 0);
                platformTilemap.SetTile(tilePos, platformTile);
            }
        }
    }
    
    private void GenerateWeaponSpawns()
    {
        weaponSpawnPositions.Clear();
        int attempts = 0;
        int maxAttempts = weaponSpawnCount * 50;
        
        while (weaponSpawnPositions.Count < weaponSpawnCount && attempts < maxAttempts)
        {
            attempts++;
            
            Vector3Int spawnPos = Vector3Int.zero;
            bool validPosition = false;
            
            if (spawnOnPlatforms && (random.Next(0, 2) == 0 || !spawnInAir))
            {
                var platform = platforms[random.Next(0, platforms.Count)];
                if (platform.type != PlatformType.Boundary && platform.length > 2)
                {
                    int xOffset = random.Next(1, platform.length - 1);
                    spawnPos = new Vector3Int(platform.x + xOffset, platform.y + 1, 0);
                    validPosition = true;
                }
            }
            else if (spawnInAir)
            {
                var platform = platforms[random.Next(0, platforms.Count)];
                if (platform.type != PlatformType.Boundary && platform.length > 2)
                {
                    int xOffset = random.Next(1, platform.length - 1);
                    int yOffset = random.Next(2, airSpawnMaxHeight + 1);
                    spawnPos = new Vector3Int(platform.x + xOffset, platform.y + yOffset, 0);
                    validPosition = true;
                }
            }
            
            if (validPosition && IsWeaponPositionValid(spawnPos))
            {
                weaponSpawnPositions.Add(spawnPos);
                weaponSpawnTilemap.SetTile(spawnPos, weaponSpawnTile);
            }
        }
    }
    
    private bool IsWeaponPositionValid(Vector3Int pos)
    {
        foreach (var weaponPos in weaponSpawnPositions)
        {
            float distance = Vector3Int.Distance(pos, weaponPos);
            if (distance < minWeaponDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    [System.Serializable]
    public class PlatformData
    {
        public int x;
        public int y;
        public int length;
        public PlatformType type;
        
        public PlatformData(int x, int y, int length, PlatformType type)
        {
            this.x = x;
            this.y = y;
            this.length = length;
            this.type = type;
        }
        
        public Vector3Int GetCenterPosition()
        {
            return new Vector3Int(x + length / 2, y, 0);
        }
    }
    
    public enum PlatformType
    {
        Normal,
        Central,
        Bridge,
        Boundary
    }
}
