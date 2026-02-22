using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attach to every weapon that exists in the world — both pre-placed pickups
/// and weapons dropped by players. When a player presses Z nearby, all stats
/// (including remaining bullets) are copied to their GunSystem and this object
/// is despawned. When a player drops a weapon, a new instance of this prefab
/// is spawned at their position with however many bullets were left.
/// </summary>
public class WeaponPickup : NetworkBehaviour
{
    [Header("Weapon Identity")]
    public Sprite weaponSprite;

    [Header("Gun Stats")]
    public int damage = 10;
    public float timeBetweenShooting = 0.5f;
    public float spreadAngle = 5f;
    public int magazineSize = 30;       // total capacity, used for UI
    public int startingBullets = 30;    // set to remaining bullets when dropped mid-use
    public int bulletsPerTap = 1;
    public bool allowButtonHold = true;

    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public Color bulletColor = Color.yellow;

    [Header("Graphics")]
    public GameObject muzzleFlashPrefab;
    public float muzzleFlashDuration = 0.1f;

    [Header("Pickup Settings")]
    public float pickupRadius = 1.5f;

    [Header("Drop Physics")]
    public float dropUpwardForce = 4f;
    public float dropSidewaysForce = 2f;

    [SerializeField] GameObject selfPrefab;

    private void Update()
    {
        if (!IsSpawned) return;

        GunSystem localPlayer = FindLocalPlayerInRange();
        if (localPlayer != null && Input.GetKeyDown(KeyCode.V))
        {
            RequestPickupServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private GunSystem FindLocalPlayerInRange()
    {
        foreach (GunSystem gs in FindObjectsOfType<GunSystem>())
        {
            if (!gs.IsOwner) continue;
            if (Vector2.Distance(transform.position, gs.transform.position) <= pickupRadius)
                return gs;
        }
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ulong requestingClientId, ServerRpcParams rpcParams = default)
    {
        GunSystem playerGunSystem = null;
        foreach (GunSystem gs in FindObjectsOfType<GunSystem>())
        {
            if (gs.OwnerClientId == requestingClientId) { playerGunSystem = gs; break; }
        }

        if (playerGunSystem == null) return;

        float dist = Vector2.Distance(transform.position, playerGunSystem.transform.position);
        if (dist > pickupRadius * 1.5f) return;

        ClientRpcParams target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { requestingClientId } }
        };

        ApplyPickupClientRpc(startingBullets, target);
        //NetworkObject.Despawn(true);
        gameObject.SetActive(false);
    }

    [ClientRpc]
    private void ApplyPickupClientRpc(int bulletsToGive, ClientRpcParams clientRpcParams = default)
    {
        GunSystem gs = null;
        foreach (GunSystem g in FindObjectsOfType<GunSystem>())
        {
            if (g.IsOwner) { gs = g; break; }
        }
        if (gs == null) return;

        // If the player is already holding a weapon, drop it before picking this up
        if (gs.HasWeapon())
            gs.Drop();

        // Copy all stats onto the player's GunSystem
        gs.damage = damage;
        gs.timeBetweenShooting = timeBetweenShooting;
        gs.spreadAngle = spreadAngle;
        gs.magazineSize = magazineSize;
        gs.bulletsPerTap = bulletsPerTap;
        gs.allowButtonHold = allowButtonHold;
        gs.bulletPrefab = bulletPrefab;
        gs.bulletSpeed = bulletSpeed;
        gs.bulletColor = bulletColor;
        gs.muzzleFlashPrefab = muzzleFlashPrefab;
        gs.muzzleFlashDuration = muzzleFlashDuration;
        gs.currentPickupPrefab = gameObject; // tells GunSystem what prefab to re-spawn on drop


        gs.SetBulletsLeft(bulletsToGive);

        // Swap the weapon sprite on the player's weapon child
        SpriteRenderer playerSR = gs.GetComponentInChildren<SpriteRenderer>();
        if (playerSR != null && weaponSprite != null)
            playerSR.sprite = weaponSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}