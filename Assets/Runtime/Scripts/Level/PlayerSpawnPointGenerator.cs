using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Player Spawn Point Generator
/// Generates balanced spawn points for battle royale mode
/// </summary>
public class PlayerSpawnPointGenerator : MonoBehaviour
{
    [Header("References")]
    public RuntimeLevelGenerator levelGenerator;
    public Transform spawnPointsParent; // Optional parent object for organization
    
    [Header("Spawn Settings")]
    public int numberOfSpawnPoints = 10;
    public bool generateOnLevelCreation = true;
    public float minSpawnDistance = 15f; // Min distance between spawn points
    
    [Header("Spawn Point Filtering")]
    public bool avoidBoundaries = true;
    public float boundaryBuffer = 10f; // Distance from edges
    public bool preferLargePlatforms = true;
    public int minimumPlatformSize = 5; // Minimum platform length for spawns
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public Color spawnPointColor = Color.green;
    public float gizmoSize = 1f;
    
    private List<Vector3> spawnPoints = new List<Vector3>();
    
    private void Start()
    {
        if (generateOnLevelCreation && levelGenerator != null)
        {
            // Wait a bit for level to fully generate
            Invoke(nameof(GenerateSpawnPoints), 0.6f);
        }
    }
    
    /// <summary>
    /// Generate spawn points based on the level layout
    /// </summary>
    public void GenerateSpawnPoints()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("[SpawnPointGenerator] No level generator assigned!");
            return;
        }
        
        ClearSpawnPoints();
        
        // Get suitable platforms
        List<RuntimeLevelGenerator.PlatformData> platforms = levelGenerator.GetPlatforms();
        List<RuntimeLevelGenerator.PlatformData> suitablePlatforms = FilterSuitablePlatforms(platforms);
        
        if (suitablePlatforms.Count == 0)
        {
            Debug.LogWarning("[SpawnPointGenerator] No suitable platforms found for spawning!");
            return;
        }
        
        // Generate spawn points
        int attempts = 0;
        int maxAttempts = numberOfSpawnPoints * 50;
        
        while (spawnPoints.Count < numberOfSpawnPoints && attempts < maxAttempts)
        {
            attempts++;
            
            // Pick a random suitable platform
            var platform = suitablePlatforms[Random.Range(0, suitablePlatforms.Count)];
            
            // Pick a random position on the platform
            int xOffset = Random.Range(1, platform.length - 1);
            Vector3Int tilePos = new Vector3Int(platform.x + xOffset, platform.y + 1, 0);
            Vector3 worldPos = levelGenerator.platformTilemap.CellToWorld(tilePos);
            worldPos.y += 0.5f; // Slight offset above platform
            
            // Check if position is valid
            if (IsSpawnPointValid(worldPos))
            {
                spawnPoints.Add(worldPos);
                CreateSpawnPointMarker(worldPos, spawnPoints.Count - 1);
            }
        }
        
        Debug.Log($"[SpawnPointGenerator] Generated {spawnPoints.Count} spawn points");
    }
    
    /// <summary>
    /// Filter platforms that are suitable for spawning
    /// </summary>
    private List<RuntimeLevelGenerator.PlatformData> FilterSuitablePlatforms(
        List<RuntimeLevelGenerator.PlatformData> allPlatforms)
    {
        List<RuntimeLevelGenerator.PlatformData> suitable = new List<RuntimeLevelGenerator.PlatformData>();
        
        foreach (var platform in allPlatforms)
        {
            // Skip boundary platforms
            if (platform.type == RuntimeLevelGenerator.PlatformType.Boundary)
                continue;
            
            // Check minimum size
            if (preferLargePlatforms && platform.length < minimumPlatformSize)
                continue;
            
            // Check boundary distance
            if (avoidBoundaries)
            {
                float distFromLeft = platform.x;
                float distFromRight = levelGenerator.levelWidth - (platform.x + platform.length);
                float distFromBottom = platform.y;
                
                if (distFromLeft < boundaryBuffer || 
                    distFromRight < boundaryBuffer || 
                    distFromBottom < boundaryBuffer)
                    continue;
            }
            
            suitable.Add(platform);
        }
        
        return suitable;
    }
    
    /// <summary>
    /// Check if spawn point is valid (not too close to other spawn points)
    /// </summary>
    private bool IsSpawnPointValid(Vector3 position)
    {
        foreach (var existingSpawn in spawnPoints)
        {
            float distance = Vector3.Distance(position, existingSpawn);
            if (distance < minSpawnDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Create a visual marker for spawn points (optional)
    /// </summary>
    private void CreateSpawnPointMarker(Vector3 position, int index)
    {
        GameObject marker = new GameObject($"SpawnPoint_{index}");
        marker.transform.position = position;
        
        if (spawnPointsParent != null)
        {
            marker.transform.SetParent(spawnPointsParent);
        }
        else
        {
            marker.transform.SetParent(transform);
        }
        
        // Add tag for easy identification
        marker.tag = "Respawn"; // Use Unity's built-in Respawn tag or create your own
        
        // Add a sprite renderer for visualization (optional)
        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        sr.color = new Color(spawnPointColor.r, spawnPointColor.g, spawnPointColor.b, 0.5f);
        
        // Create a simple circle sprite programmatically
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        Vector2 center = new Vector2(16, 16);
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * 32 + x] = dist < 12 ? spawnPointColor : Color.clear;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
    }
    
    /// <summary>
    /// Get all spawn points
    /// </summary>
    public List<Vector3> GetSpawnPoints()
    {
        return new List<Vector3>(spawnPoints);
    }
    
    /// <summary>
    /// Get a random spawn point
    /// </summary>
    public Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[SpawnPointGenerator] No spawn points available!");
            return Vector3.zero;
        }
        
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
    
    /// <summary>
    /// Get the furthest spawn point from a given position
    /// </summary>
    public Vector3 GetFurthestSpawnPoint(Vector3 fromPosition)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[SpawnPointGenerator] No spawn points available!");
            return Vector3.zero;
        }
        
        Vector3 furthest = spawnPoints[0];
        float maxDistance = 0f;
        
        foreach (var point in spawnPoints)
        {
            float distance = Vector3.Distance(fromPosition, point);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthest = point;
            }
        }
        
        return furthest;
    }
    
    /// <summary>
    /// Clear all spawn points
    /// </summary>
    public void ClearSpawnPoints()
    {
        spawnPoints.Clear();
        
        // Clean up existing markers
        if (spawnPointsParent != null)
        {
            foreach (Transform child in spawnPointsParent)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || spawnPoints.Count == 0) return;
        
        Gizmos.color = spawnPointColor;
        
        foreach (var point in spawnPoints)
        {
            Gizmos.DrawWireSphere(point, gizmoSize);
            Gizmos.DrawLine(point, point + Vector3.up * gizmoSize * 1.5f);
        }
    }
}
