using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

/// <summary>
/// Quick Setup Utility - Creates a complete level generation system with one click
/// </summary>
public class LevelGeneratorQuickSetup : EditorWindow
{
    private TileBase platformTile;
    private TileBase weaponSpawnTile;
    
    [MenuItem("Tools/Quick Setup Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelGeneratorQuickSetup>("Quick Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Level Generator Quick Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This will create the tilemap structure for editor-time level generation:\n" +
            "• Grid with 2 Tilemaps (Platforms & Weapons)\n" +
            "• Ready to use with Tools > Procedural Level Generator", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        GUILayout.Label("Required Tiles", EditorStyles.boldLabel);
        platformTile = (TileBase)EditorGUILayout.ObjectField(
            "Platform Tile", platformTile, typeof(TileBase), false);
        weaponSpawnTile = (TileBase)EditorGUILayout.ObjectField(
            "Weapon Spawn Tile", weaponSpawnTile, typeof(TileBase), false);
        
        GUILayout.Space(10);
        
        if (platformTile == null || weaponSpawnTile == null)
        {
            EditorGUILayout.HelpBox(
                "Please assign both tiles before setup.\n\n" +
                "To create tiles:\n" +
                "1. Right-click in Assets\n" +
                "2. Create > 2D > Tiles > Tile\n" +
                "3. Assign a sprite to the tile", 
                MessageType.Warning);
        }
        
        EditorGUI.BeginDisabledGroup(platformTile == null || weaponSpawnTile == null);
        
        if (GUILayout.Button("Create Tilemap Setup", GUILayout.Height(40)))
        {
            CreateTilemapSetup();
        }
        
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "After creating the setup:\n" +
            "1. Go to Tools > Procedural Level Generator\n" +
            "2. The tilemaps and tiles will auto-populate\n" +
            "3. Click 'Generate Level' to create your level in the editor!", 
            MessageType.Info);
    }
    
    private void CreateTilemapSetup()
    {
        // Create Grid
        GameObject grid = CreateGridSetup();
        
        Debug.Log("[Quick Setup] Tilemap setup created! Now open Tools > Procedural Level Generator to generate your level.");
        
        // Auto-open the level generator window
        EditorApplication.delayCall += () => {
            ProceduralLevelGenerator.ShowWindow();
        };
    }
    
    private GameObject CreateGridSetup()
    {
        // Create Grid
        GameObject grid = new GameObject("Grid");
        Grid gridComponent = grid.AddComponent<Grid>();
        gridComponent.cellSize = new Vector3(1, 1, 0);
        
        // Create Platform Tilemap
        GameObject platformObj = new GameObject("PlatformTilemap");
        platformObj.transform.SetParent(grid.transform);
        Tilemap platformTilemap = platformObj.AddComponent<Tilemap>();
        TilemapRenderer platformRenderer = platformObj.AddComponent<TilemapRenderer>();
        platformRenderer.sortingLayerName = "Default";
        platformRenderer.sortingOrder = 0;
        
        // Add Tilemap Collider for platforms
        TilemapCollider2D platformCollider = platformObj.AddComponent<TilemapCollider2D>();
        
        // Create Weapon Spawn Tilemap
        GameObject weaponObj = new GameObject("WeaponSpawnTilemap");
        weaponObj.transform.SetParent(grid.transform);
        Tilemap weaponTilemap = weaponObj.AddComponent<Tilemap>();
        TilemapRenderer weaponRenderer = weaponObj.AddComponent<TilemapRenderer>();
        weaponRenderer.sortingLayerName = "Default";
        weaponRenderer.sortingOrder = 1; // Render above platforms
        
        Debug.Log("[Quick Setup] Grid with tilemaps created!");
        
        Selection.activeGameObject = grid;
        return grid;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom Inspector for Runtime Level Generator - adds a Generate button in play mode
/// </summary>
[CustomEditor(typeof(RuntimeLevelGenerator))]
public class RuntimeLevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        RuntimeLevelGenerator generator = (RuntimeLevelGenerator)target;
        
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Generate New Level", GUILayout.Height(30)))
            {
                generator.GenerateLevel();
            }
            
            if (GUILayout.Button("Clear Level", GUILayout.Height(25)))
            {
                generator.ClearLevel();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to test level generation at runtime.\n" +
                "Or use Tools > Procedural Level Generator for editor-time generation.",
                MessageType.Info);
        }
    }
}

/// <summary>
/// Custom Inspector for Weapon Spawner - adds spawn button
/// </summary>
[CustomEditor(typeof(WeaponSpawner))]
public class WeaponSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        WeaponSpawner spawner = (WeaponSpawner)target;
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Active Weapons", spawner.GetActiveWeaponCount().ToString());
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Spawn Weapons", GUILayout.Height(30)))
            {
                spawner.SpawnWeapons();
            }
            
            if (GUILayout.Button("Clear Weapons", GUILayout.Height(25)))
            {
                spawner.ClearWeapons();
            }
        }
    }
}

/// <summary>
/// Custom Inspector for Player Spawn Point Generator
/// </summary>
[CustomEditor(typeof(PlayerSpawnPointGenerator))]
public class PlayerSpawnPointGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        PlayerSpawnPointGenerator generator = (PlayerSpawnPointGenerator)target;
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Spawn Points", generator.GetSpawnPoints().Count.ToString());
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Generate Spawn Points", GUILayout.Height(30)))
            {
                generator.GenerateSpawnPoints();
            }
            
            if (GUILayout.Button("Clear Spawn Points", GUILayout.Height(25)))
            {
                generator.ClearSpawnPoints();
            }
        }
    }
}
#endif
