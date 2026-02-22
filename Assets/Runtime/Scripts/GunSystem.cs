using UnityEngine;
using Unity.Netcode;
using TMPro;

[RequireComponent(typeof(Weapon))]
public class GunSystem : NetworkBehaviour
{
    [Header("Gun Stats")]
    public int damage = 10;
    public float timeBetweenShooting = 0.5f;
    public float spreadAngle = 5f;
    public int magazineSize = 30;
    public int bulletsPerTap = 1;
    public bool allowButtonHold = true;

    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;
    public Color bulletColor = Color.yellow;

    [Header("Graphics")]
    public GameObject muzzleFlashPrefab;
    public float muzzleFlashDuration = 0.1f;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    // Set by WeaponPickup when player picks up a gun — used to re-spawn on drop
    public GameObject currentPickupPrefab;

    // Private state
    private int bulletsLeft;
    private int bulletsShot;
    private bool shooting;
    private bool readyToShoot = true;

    public PlayerMovement playerMovement;

    private void Awake()
    {
        bulletsLeft = 0; // starts empty — no weapon until first pickup
        if (!IsOwner) return;
        showAmmoTextServerRpc();
        playerMovement = FindObjectOfType<PlayerMovement>();
    }

    [ServerRpc]
    public void showAmmoTextServerRpc()
    {
        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(true);
            print("hit");
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleInput();
        UpdateUI();
    }

    private void HandleInput()
    {
        // Shoot input
        if (allowButtonHold)
            shooting = Input.GetKey(KeyCode.Z);
        else
            shooting = Input.GetKeyDown(KeyCode.Z);

        // Only shoot if holding a weapon and have ammo
        if (readyToShoot && shooting && bulletsLeft > 0 && HasWeapon())
        {
            bulletsShot = bulletsPerTap;
            Shoot();
        }

        // Drop weapon with Q
        if (Input.GetKeyDown(KeyCode.V) && HasWeapon())
        {
            print("finna drop");
            Drop();
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        Vector2 shootDirection = transform.parent.localScale.x > 0 ? Vector2.right : Vector2.left;

        float spreadOffset = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRotation = Quaternion.Euler(0, 0, spreadOffset);
        Vector2 finalDirection = spreadRotation * shootDirection;

        ShootServerRpc(firePoint.position, finalDirection);

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, Quaternion.identity);
            Destroy(flash, muzzleFlashDuration);
        }

        bulletsLeft--;
        bulletsShot--;

        Invoke(nameof(ResetShot), timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0)
            Invoke(nameof(Shoot), timeBetweenShooting);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPosition, Vector2 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.GetComponent<SpriteRenderer>().color = bulletColor;

        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        netObj.Spawn();

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.Initialize(direction, bulletSpeed, damage, OwnerClientId);
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    /// <summary>
    /// Drops the currently held weapon. Spawns a WeaponPickup in the world
    /// with the remaining bullet count, then clears this GunSystem.
    /// Called automatically when picking up a new weapon while already holding one.
    /// </summary>
    public void Drop()
    {
        if (!HasWeapon() || currentPickupPrefab == null) return;

        // Gather the weapon's current sprite so the dropped pickup looks right
        Sprite currentSprite = null;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) currentSprite = sr.sprite;

        SpawnDroppedWeaponServerRpc(
            transform.position,
            bulletsLeft,
            damage,
            timeBetweenShooting,
            spreadAngle,
            magazineSize,
            bulletsPerTap,
            allowButtonHold,
            bulletSpeed,
            bulletColor,
            muzzleFlashDuration
        );

        ClearWeapon();
    }

    [ServerRpc]
    private void SpawnDroppedWeaponServerRpc(
        Vector3 position,
        int bulletsRemaining,
        int dmg,
        float shootDelay,
        float spread,
        int magSize,
        int perTap,
        bool holdAllowed,
        float bSpeed,
        Color bColor,
        float muzzleDuration)
    {
        if (currentPickupPrefab == null) return;

        // Spawn a little above and to the side so it doesn't overlap the player
        Vector3 spawnPos = position + new Vector3(0f, 0.5f, 0f);

        GameObject dropped = Instantiate(currentPickupPrefab, spawnPos, Quaternion.identity);
        WeaponPickup wp = dropped.GetComponent<WeaponPickup>();

        wp.damage = dmg;
        wp.timeBetweenShooting = shootDelay;
        wp.spreadAngle = spread;
        wp.magazineSize = magSize;
        wp.startingBullets = bulletsRemaining; // carries over remaining ammo
        wp.bulletsPerTap = perTap;
        wp.allowButtonHold = holdAllowed;
        wp.bulletSpeed = bSpeed;
        wp.bulletColor = bColor;
        wp.muzzleFlashDuration = muzzleDuration;
        // bulletPrefab and muzzleFlashPrefab are already set on the prefab in the Inspector

        // Give it a small upward kick so it visually pops out
        Rigidbody2D rb = dropped.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float side = Random.Range(-1f, 1f);
            rb.AddForce(new Vector2(side * 2f, 4f), ForceMode2D.Impulse);
        }

        dropped.gameObject.SetActive(true);
        dropped.GetComponent<NetworkObject>().Spawn();
        Destroy(currentPickupPrefab);
    }

    // ── Public helpers ────────────────────────────────────────────────────────

    public bool HasWeapon() => bulletPrefab != null;

    public void SetBulletsLeft(int amount)
    {
        bulletsLeft = amount;
    }

    /// <summary>Resets GunSystem to its empty, no-weapon state.</summary>
    public void ClearWeapon()
    {
        bulletPrefab = null;
        currentPickupPrefab = null;
        bulletsLeft = 0;
        magazineSize = 0;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sprite = null;

        if (ammoText != null) ammoText.text = "";
    }

    private void UpdateUI()
    {
        if (ammoText == null) return;
        ammoText.gameObject.SetActive(HasWeapon());
        ammoText.text = HasWeapon() ? $"{bulletsLeft} / {magazineSize}" : "";
    }
}