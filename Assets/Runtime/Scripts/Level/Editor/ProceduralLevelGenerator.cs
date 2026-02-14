using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Procedural Level Generator for 2D Platformer Battle Royale
/// Generates platform layouts and weapon spawn positions using tilemaps
/// </summary>
public class ProceduralLevelGenerator : EditorWindow
{
    [System.Serializable]
    public class GeneratorPreset
    {
        public string presetName = "New Preset";
        public int levelWidth = 100;
        public int levelHeight = 60;
        public int playerJumpHeight = 5;
        public int playerJumpDistance = 8;
        public int minPlatformSpacing = 2;
        public int minPlatformLength = 3;
        public int maxPlatformLength = 15;
        public int platformDensity = 35;
        public bool ensureConnectivity = true;
        public int minHeightVariation = 3;
        public int maxHeightVariation = 8;
        public int weaponSpawnCount = 20;
        public int minWeaponDistance = 10;
        public bool spawnOnPlatforms = true;
        public bool spawnInAir = true;
        public int airSpawnMaxHeight = 3;
        public bool createBoundaryWalls = true;
        public int boundaryWallHeight = 10;
        public bool createCentralPlatform = true;
        public int centralPlatformSize = 12;
    }
    
    [Header("Level Dimensions")]
    [SerializeField] private int levelWidth = 100;
    [SerializeField] private int levelHeight = 60;
    
    [Header("Player Movement Parameters")]
    [SerializeField] private int playerJumpHeight = 5;
    [SerializeField] private int playerJumpDistance = 8;
    [SerializeField] private int minPlatformSpacing = 2;
    
    [Header("Platform Generation")]
    [SerializeField] private int minPlatformLength = 3;
    [SerializeField] private int maxPlatformLength = 15;
    [SerializeField] private int platformDensity = 35; // Percentage
    [SerializeField] private bool ensureConnectivity = true;
    [SerializeField] private int minHeightVariation = 3;
    [SerializeField] private int maxHeightVariation = 8;
    
    [Header("Weapon Spawn Settings")]
    [SerializeField] private int weaponSpawnCount = 20;
    [SerializeField] private int minWeaponDistance = 10; // Min distance between weapons
    [SerializeField] private bool spawnOnPlatforms = true;
    [SerializeField] private bool spawnInAir = true;
    [SerializeField] private int airSpawnMaxHeight = 3; // Max height above platforms
    
    [Header("Arena Design")]
    [SerializeField] private bool createBoundaryWalls = true;
    [SerializeField] private int boundaryWallHeight = 10;
    [SerializeField] private bool createCentralPlatform = true;
    [SerializeField] private int centralPlatformSize = 12;
    
    [Header("References")]
    [SerializeField] private Tilemap platformTilemap;
    [SerializeField] private Tilemap weaponSpawnTilemap;
    [SerializeField] private TileBase platformTile;
    [SerializeField] private TileBase weaponSpawnTile;
    
    [Header("Generation Settings")]
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useRandomSeed = true;
    
    private System.Random random;
    private List<PlatformData> platforms = new List<PlatformData>();
    private Vector2 scrollPosition;
    
    // Preset management
    private List<GeneratorPreset> presets = new List<GeneratorPreset>();
    private int selectedPresetIndex = -1;
    private bool showPresets = false;
    
    [MenuItem("Tools/Procedural Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<ProceduralLevelGenerator>("Level Generator");
    }
    
    private void OnEnable()
    {
        // Auto-find tilemaps and tiles in scene when window opens
        AutoFindReferences();
        LoadPresets();
    }
    
    /// <summary>
    /// Automatically find tilemap and tile references in the scene
    /// </summary>
    private void AutoFindReferences()
    {
        // Find tilemaps if not assigned
        if (platformTilemap == null || weaponSpawnTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (var tilemap in tilemaps)
            {
                if (tilemap.name.ToLower().Contains("platform") && platformTilemap == null)
                {
                    platformTilemap = tilemap;
                }
                else if (tilemap.name.ToLower().Contains("weapon") && weaponSpawnTilemap == null)
                {
                    weaponSpawnTilemap = tilemap;
                }
            }
        }
        
        // Find tiles if not assigned
        if (platformTile == null || weaponSpawnTile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TileBase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                
                if (tile != null)
                {
                    string tileName = tile.name.ToLower();
                    if (tileName.Contains("platform") && platformTile == null)
                    {
                        platformTile = tile;
                    }
                    else if ((tileName.Contains("weapon") || tileName.Contains("spawn")) && weaponSpawnTile == null)
                    {
                        weaponSpawnTile = tile;
                    }
                }
            }
        }
    }
    
    private void SavePresets()
    {
        string json = JsonUtility.ToJson(new PresetList { presets = this.presets }, true);
        EditorPrefs.SetString("LevelGeneratorPresets", json);
    }
    
    private void LoadPresets()
    {
        string json = EditorPrefs.GetString("LevelGeneratorPresets", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                PresetList list = JsonUtility.FromJson<PresetList>(json);
                presets = list.presets ?? new List<GeneratorPreset>();
            }
            catch
            {
                presets = new List<GeneratorPreset>();
            }
        }
        
        // Add default presets if empty
        if (presets.Count == 0)
        {
            AddDefaultPresets();
        }
    }
    
    private void AddDefaultPresets()
    {
        // Battle Royale preset
        presets.Add(new GeneratorPreset
        {
            presetName = "Battle Royale",
            levelWidth = 120,
            levelHeight = 70,
            platformDensity = 35,
            weaponSpawnCount = 25,
            createCentralPlatform = true,
            ensureConnectivity = true
        });
        
        // Dense Combat preset
        presets.Add(new GeneratorPreset
        {
            presetName = "Dense Combat",
            levelWidth = 80,
            levelHeight = 50,
            platformDensity = 50,
            maxPlatformLength = 10,
            weaponSpawnCount = 15,
            createCentralPlatform = false
        });
        
        // Parkour preset
        presets.Add(new GeneratorPreset
        {
            presetName = "Parkour Challenge",
            levelWidth = 100,
            levelHeight = 60,
            platformDensity = 25,
            minPlatformLength = 2,
            maxPlatformLength = 6,
            maxHeightVariation = 12,
            weaponSpawnCount = 10,
            spawnInAir = true,
            airSpawnMaxHeight = 5
        });
        
        SavePresets();
    }
    
    private void ApplyPreset(GeneratorPreset preset)
    {
        levelWidth = preset.levelWidth;
        levelHeight = preset.levelHeight;
        playerJumpHeight = preset.playerJumpHeight;
        playerJumpDistance = preset.playerJumpDistance;
        minPlatformSpacing = preset.minPlatformSpacing;
        minPlatformLength = preset.minPlatformLength;
        maxPlatformLength = preset.maxPlatformLength;
        platformDensity = preset.platformDensity;
        ensureConnectivity = preset.ensureConnectivity;
        minHeightVariation = preset.minHeightVariation;
        maxHeightVariation = preset.maxHeightVariation;
        weaponSpawnCount = preset.weaponSpawnCount;
        minWeaponDistance = preset.minWeaponDistance;
        spawnOnPlatforms = preset.spawnOnPlatforms;
        spawnInAir = preset.spawnInAir;
        airSpawnMaxHeight = preset.airSpawnMaxHeight;
        createBoundaryWalls = preset.createBoundaryWalls;
        boundaryWallHeight = preset.boundaryWallHeight;
        createCentralPlatform = preset.createCentralPlatform;
        centralPlatformSize = preset.centralPlatformSize;
    }
    
    private GeneratorPreset CreatePresetFromCurrent(string name)
    {
        return new GeneratorPreset
        {
            presetName = name,
            levelWidth = levelWidth,
            levelHeight = levelHeight,
            playerJumpHeight = playerJumpHeight,
            playerJumpDistance = playerJumpDistance,
            minPlatformSpacing = minPlatformSpacing,
            minPlatformLength = minPlatformLength,
            maxPlatformLength = maxPlatformLength,
            platformDensity = platformDensity,
            ensureConnectivity = ensureConnectivity,
            minHeightVariation = minHeightVariation,
            maxHeightVariation = maxHeightVariation,
            weaponSpawnCount = weaponSpawnCount,
            minWeaponDistance = minWeaponDistance,
            spawnOnPlatforms = spawnOnPlatforms,
            spawnInAir = spawnInAir,
            airSpawnMaxHeight = airSpawnMaxHeight,
            createBoundaryWalls = createBoundaryWalls,
            boundaryWallHeight = boundaryWallHeight,
            createCentralPlatform = createCentralPlatform,
            centralPlatformSize = centralPlatformSize
        };
    }
    
    [System.Serializable]
    private class PresetList
    {
        public List<GeneratorPreset> presets;
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Procedural Level Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Preset Section
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets)
        {
            EditorGUI.indentLevel++;
            
            // Preset selection
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Load Preset:", GUILayout.Width(80));
            
            string[] presetNames = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
            {
                presetNames[i] = presets[i].presetName;
            }
            
            int newIndex = EditorGUILayout.Popup(selectedPresetIndex, presetNames);
            if (newIndex != selectedPresetIndex)
            {
                selectedPresetIndex = newIndex;
                if (selectedPresetIndex >= 0 && selectedPresetIndex < presets.Count)
                {
                    ApplyPreset(presets[selectedPresetIndex]);
                }
            }
            
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                if (selectedPresetIndex >= 0 && selectedPresetIndex < presets.Count)
                {
                    ApplyPreset(presets[selectedPresetIndex]);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Save current as preset
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Current Settings as Preset"))
            {
                SaveCurrentAsPreset();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
            GUILayout.Space(5);
        }
        
        // Level Dimensions
        GUILayout.Label("Level Dimensions", EditorStyles.boldLabel);
        levelWidth = EditorGUILayout.IntSlider("Level Width", levelWidth, 50, 200);
        levelHeight = EditorGUILayout.IntSlider("Level Height", levelHeight, 30, 100);
        GUILayout.Space(5);
        
        // Player Movement
        GUILayout.Label("Player Movement Parameters", EditorStyles.boldLabel);
        playerJumpHeight = EditorGUILayout.IntSlider("Jump Height", playerJumpHeight, 3, 10);
        playerJumpDistance = EditorGUILayout.IntSlider("Jump Distance", playerJumpDistance, 5, 15);
        minPlatformSpacing = EditorGUILayout.IntSlider("Min Platform Spacing", minPlatformSpacing, 1, 5);
        GUILayout.Space(5);
        
        // Platform Generation
        GUILayout.Label("Platform Generation", EditorStyles.boldLabel);
        minPlatformLength = EditorGUILayout.IntSlider("Min Platform Length", minPlatformLength, 2, 10);
        maxPlatformLength = EditorGUILayout.IntSlider("Max Platform Length", maxPlatformLength, minPlatformLength, 25);
        platformDensity = EditorGUILayout.IntSlider("Platform Density %", platformDensity, 10, 70);
        ensureConnectivity = EditorGUILayout.Toggle("Ensure Connectivity", ensureConnectivity);
        minHeightVariation = EditorGUILayout.IntSlider("Min Height Variation", minHeightVariation, 2, 8);
        maxHeightVariation = EditorGUILayout.IntSlider("Max Height Variation", maxHeightVariation, minHeightVariation, 15);
        GUILayout.Space(5);
        
        // Weapon Spawns
        GUILayout.Label("Weapon Spawn Settings", EditorStyles.boldLabel);
        weaponSpawnCount = EditorGUILayout.IntSlider("Weapon Spawn Count", weaponSpawnCount, 5, 50);
        minWeaponDistance = EditorGUILayout.IntSlider("Min Weapon Distance", minWeaponDistance, 5, 20);
        spawnOnPlatforms = EditorGUILayout.Toggle("Spawn On Platforms", spawnOnPlatforms);
        spawnInAir = EditorGUILayout.Toggle("Spawn In Air", spawnInAir);
        if (spawnInAir)
        {
            airSpawnMaxHeight = EditorGUILayout.IntSlider("  Max Air Height", airSpawnMaxHeight, 1, 8);
        }
        GUILayout.Space(5);
        
        // Arena Design
        GUILayout.Label("Arena Design", EditorStyles.boldLabel);
        createBoundaryWalls = EditorGUILayout.Toggle("Create Boundary Walls", createBoundaryWalls);
        if (createBoundaryWalls)
        {
            boundaryWallHeight = EditorGUILayout.IntSlider("  Wall Height", boundaryWallHeight, 5, 20);
        }
        createCentralPlatform = EditorGUILayout.Toggle("Create Central Platform", createCentralPlatform);
        if (createCentralPlatform)
        {
            centralPlatformSize = EditorGUILayout.IntSlider("  Platform Size", centralPlatformSize, 6, 25);
        }
        GUILayout.Space(5);
        
        // References
        GUILayout.Label("Tilemap References", EditorStyles.boldLabel);
        platformTilemap = (Tilemap)EditorGUILayout.ObjectField("Platform Tilemap", platformTilemap, typeof(Tilemap), true);
        weaponSpawnTilemap = (Tilemap)EditorGUILayout.ObjectField("Weapon Tilemap", weaponSpawnTilemap, typeof(Tilemap), true);
        platformTile = (TileBase)EditorGUILayout.ObjectField("Platform Tile", platformTile, typeof(TileBase), false);
        weaponSpawnTile = (TileBase)EditorGUILayout.ObjectField("Weapon Spawn Tile", weaponSpawnTile, typeof(TileBase), false);
        
        if (GUILayout.Button("Auto-Find References"))
        {
            AutoFindReferences();
            Repaint();
        }
        
        GUILayout.Space(5);
        
        // Generation Settings
        GUILayout.Label("Generation Settings", EditorStyles.boldLabel);
        useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", useRandomSeed);
        if (!useRandomSeed)
        {
            seed = EditorGUILayout.IntField("Seed", seed);
        }
        GUILayout.Space(10);
        
        // Buttons
        EditorGUI.BeginDisabledGroup(platformTilemap == null || weaponSpawnTilemap == null || 
                                     platformTile == null || weaponSpawnTile == null);
        
        if (GUILayout.Button("Generate Level", GUILayout.Height(35)))
        {
            GenerateLevel();
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("Clear Level", GUILayout.Height(25)))
        {
            ClearLevel();
        }
        
        GUILayout.Space(10);
        
        if (platformTilemap == null || weaponSpawnTilemap == null || platformTile == null || weaponSpawnTile == null)
        {
            EditorGUILayout.HelpBox("Please assign all tilemap references before generating.", MessageType.Warning);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void SaveCurrentAsPreset()
    {
        // Simple dialog for preset name
        string presetName = "Custom Preset " + (presets.Count + 1);
        
        GeneratorPreset newPreset = CreatePresetFromCurrent(presetName);
        presets.Add(newPreset);
        SavePresets();
        
        selectedPresetIndex = presets.Count - 1;
        
        Debug.Log($"Saved preset: {presetName}");
    }
    
    private void GenerateLevel()
    {
        // Initialize random
        if (useRandomSeed)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        random = new System.Random(seed);
        
        Debug.Log($"Generating level with seed: {seed}");
        
        // Clear existing level
        ClearLevel();
        
        platforms.Clear();
        
        // Generate level structure
        GenerateBoundaries();
        GenerateCentralPlatform();
        GeneratePlatforms();
        
        if (ensureConnectivity)
        {
            EnsurePlatformConnectivity();
        }
        
        // Place platforms on tilemap
        DrawPlatforms();
        
        // Generate weapon spawns
        GenerateWeaponSpawns();
        
        Debug.Log($"Level generation complete! Generated {platforms.Count} platforms and {weaponSpawnCount} weapon spawns.");
    }
    
    private void GenerateBoundaries()
    {
        if (!createBoundaryWalls) return;
        
        // Floor
        platforms.Add(new PlatformData(0, 0, levelWidth, PlatformType.Boundary));
        
        // Left wall
        for (int y = 0; y <= boundaryWallHeight; y++)
        {
            platforms.Add(new PlatformData(0, y, 1, PlatformType.Boundary));
        }
        
        // Right wall
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
        
        int currentHeight = 5; // Start above floor
        
        while (currentTiles < targetTiles && attempts < maxAttempts)
        {
            attempts++;
            
            // Random platform length
            int platformLength = random.Next(minPlatformLength, maxPlatformLength + 1);
            
            // Random X position
            int platformX = random.Next(5, levelWidth - platformLength - 5);
            
            // Vary height
            int heightChange = random.Next(-maxHeightVariation, maxHeightVariation + 1);
            currentHeight = Mathf.Clamp(currentHeight + heightChange, 
                                       playerJumpHeight + 2, 
                                       levelHeight - 5);
            
            // Check if platform is valid
            if (IsPlatformValid(platformX, currentHeight, platformLength))
            {
                platforms.Add(new PlatformData(platformX, currentHeight, platformLength, PlatformType.Normal));
                currentTiles += platformLength;
                
                // Reset height occasionally for variety
                if (random.Next(0, 100) < 20)
                {
                    currentHeight = random.Next(playerJumpHeight + 2, levelHeight / 2);
                }
            }
        }
    }
    
    private bool IsPlatformValid(int x, int y, int length)
    {
        // Check collision with existing platforms
        foreach (var platform in platforms)
        {
            if (platform.type == PlatformType.Boundary) continue;
            
            // Check horizontal overlap
            bool horizontalOverlap = !(x + length < platform.x || x > platform.x + platform.length);
            
            // Check vertical proximity
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
        // Sort platforms by X position
        platforms.Sort((a, b) => a.x.CompareTo(b.x));
        
        // Connect gaps that are too large
        for (int i = 0; i < platforms.Count - 1; i++)
        {
            var current = platforms[i];
            var next = platforms[i + 1];
            
            if (current.type == PlatformType.Boundary || next.type == PlatformType.Boundary) continue;
            
            float horizontalDist = next.x - (current.x + current.length);
            float verticalDist = Mathf.Abs(next.y - current.y);
            
            // If gap is too large, add stepping stone
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
        List<Vector3Int> weaponPositions = new List<Vector3Int>();
        int attempts = 0;
        int maxAttempts = weaponSpawnCount * 50;
        
        while (weaponPositions.Count < weaponSpawnCount && attempts < maxAttempts)
        {
            attempts++;
            
            Vector3Int spawnPos = Vector3Int.zero;
            bool validPosition = false;
            
            // Try to spawn on platform
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
            // Try to spawn in air above platform
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
            
            if (validPosition && IsWeaponPositionValid(spawnPos, weaponPositions))
            {
                weaponPositions.Add(spawnPos);
                weaponSpawnTilemap.SetTile(spawnPos, weaponSpawnTile);
            }
        }
        
        Debug.Log($"Placed {weaponPositions.Count} weapon spawns");
    }
    
    private bool IsWeaponPositionValid(Vector3Int pos, List<Vector3Int> existingWeapons)
    {
        foreach (var weaponPos in existingWeapons)
        {
            float distance = Vector3Int.Distance(pos, weaponPos);
            if (distance < minWeaponDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    private void ClearLevel()
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
    
    [System.Serializable]
    private class PlatformData
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
    }
    
    private enum PlatformType
    {
        Normal,
        Central,
        Bridge,
        Boundary
    }
}
