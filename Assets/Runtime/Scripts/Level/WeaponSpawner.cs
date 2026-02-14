using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Weapon Spawner - Spawns weapon pickups at the generated weapon spawn positions
/// </summary>
public class WeaponSpawner : MonoBehaviour
{
    [Header("References")]
    public RuntimeLevelGenerator levelGenerator;
    
    [Header("Weapon Prefabs")]
    public GameObject[] weaponPrefabs;
    
    [Header("Spawn Settings")]
    public bool spawnOnLevelGeneration = true;
    public float spawnDelay = 0.5f;
    public bool randomizeWeaponTypes = true;
    
    [Header("Respawn Settings")]
    public bool enableRespawn = true;
    public float respawnTime = 30f;
    
    private List<WeaponPickup> spawnedWeapons = new List<WeaponPickup>();
    
    private void Start()
    {
        if (spawnOnLevelGeneration && levelGenerator != null)
        {
            // Wait for level to generate
            Invoke(nameof(SpawnWeapons), spawnDelay);
        }
    }
    
    /// <summary>
    /// Spawn weapons at all generated spawn points
    /// </summary>
    public void SpawnWeapons()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("[WeaponSpawner] No level generator assigned!");
            return;
        }
        
        if (weaponPrefabs == null || weaponPrefabs.Length == 0)
        {
            Debug.LogError("[WeaponSpawner] No weapon prefabs assigned!");
            return;
        }
        
        // Clear existing weapons
        ClearWeapons();
        
        // Get spawn positions from the level generator
        List<Vector3> spawnPositions = levelGenerator.GetWeaponSpawnWorldPositions();
        
        Debug.Log($"[WeaponSpawner] Spawning {spawnPositions.Count} weapons");
        
        foreach (Vector3 spawnPos in spawnPositions)
        {
            SpawnWeaponAt(spawnPos);
        }
    }
    
    /// <summary>
    /// Spawn a weapon at a specific position
    /// </summary>
    private void SpawnWeaponAt(Vector3 position)
    {
        // Select weapon prefab
        GameObject prefab = randomizeWeaponTypes 
            ? weaponPrefabs[Random.Range(0, weaponPrefabs.Length)]
            : weaponPrefabs[0];
        
        // Instantiate weapon
        GameObject weaponObj = Instantiate(prefab, position, Quaternion.identity, transform);
        
        // Setup weapon pickup component
        WeaponPickup pickup = weaponObj.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            pickup = weaponObj.AddComponent<WeaponPickup>();
        }
        
        pickup.spawner = this;
        pickup.spawnPosition = position;
        pickup.respawnTime = respawnTime;
        pickup.enableRespawn = enableRespawn;
        
        spawnedWeapons.Add(pickup);
    }
    
    /// <summary>
    /// Called by WeaponPickup when a weapon needs to respawn
    /// </summary>
    public void RespawnWeapon(WeaponPickup pickup)
    {
        if (!enableRespawn) return;
        
        SpawnWeaponAt(pickup.spawnPosition);
    }
    
    /// <summary>
    /// Clear all spawned weapons
    /// </summary>
    public void ClearWeapons()
    {
        foreach (var weapon in spawnedWeapons)
        {
            if (weapon != null && weapon.gameObject != null)
            {
                Destroy(weapon.gameObject);
            }
        }
        
        spawnedWeapons.Clear();
    }
    
    /// <summary>
    /// Get the number of weapons currently spawned
    /// </summary>
    public int GetActiveWeaponCount()
    {
        return spawnedWeapons.Count;
    }
}

/// <summary>
/// Weapon Pickup - Attach to weapon prefabs or it will be added automatically
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [HideInInspector] public WeaponSpawner spawner;
    [HideInInspector] public Vector3 spawnPosition;
    [HideInInspector] public float respawnTime = 30f;
    [HideInInspector] public bool enableRespawn = true;
    
    public string weaponName = "Weapon";
    public Sprite weaponIcon;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;
    private bool isPickedUp = false;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<Collider2D>();
        
        // Add collider if missing
        if (pickupCollider == null)
        {
            pickupCollider = gameObject.AddComponent<BoxCollider2D>();
            ((BoxCollider2D)pickupCollider).isTrigger = true;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;
        
        // Check if player picked up the weapon
        if (other.CompareTag("Player"))
        {
            PickupWeapon(other.gameObject);
        }
    }
    
    private void PickupWeapon(GameObject player)
    {
        isPickedUp = true;
        
        Debug.Log($"[WeaponPickup] Player picked up {weaponName}");
        
        // TODO: Add weapon to player's inventory here
        // Example: player.GetComponent<PlayerInventory>().AddWeapon(weaponName);
        
        // Hide the weapon
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        
        if (pickupCollider != null)
            pickupCollider.enabled = false;
        
        // Schedule respawn
        if (enableRespawn && spawner != null)
        {
            Invoke(nameof(Respawn), respawnTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Respawn()
    {
        if (spawner != null)
        {
            spawner.RespawnWeapon(this);
            Destroy(gameObject);
        }
    }
}
