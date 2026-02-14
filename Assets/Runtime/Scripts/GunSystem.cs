using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GunSystem : NetworkBehaviour
{
    [Header("Gun Stats")]
    public int damage = 10;
    public float timeBetweenShooting = 0.5f;
    public float spreadAngle = 5f;
    public float reloadTime = 2f;
    public int magazineSize = 30;
    public int bulletsPerTap = 1;
    public bool allowButtonHold = true;

    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;

    [Header("Graphics")]
    public GameObject muzzleFlashPrefab;
    public float muzzleFlashDuration = 0.1f;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    // Private variables
    private int bulletsLeft;
    private int bulletsShot;
    private bool shooting;
    private bool readyToShoot = true;
    private bool reloading;

    private void Awake()
    {
        bulletsLeft = magazineSize;
        if (!IsOwner) return;
        ammoText.gameObject.SetActive(true);
    }

    private void Update()
    {
        // Only allow input from the owner
        if (!IsOwner) return;

        HandleInput();
        UpdateUI();
    }

    private void HandleInput()
    {
        // Shooting input
        if (allowButtonHold)
            shooting = Input.GetKey(KeyCode.Z);
        else
            shooting = Input.GetKeyDown(KeyCode.Z);

        // Reload input
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading)
        {
            Reload();
        }

        // Shoot
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            Shoot();
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        // Determine shoot direction based on player facing
        Vector2 shootDirection = transform.parent.localScale.x > 0 ? Vector2.right : Vector2.left;

        // Apply spread
        float spreadOffset = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRotation = Quaternion.Euler(0, 0, spreadOffset);
        Vector2 finalDirection = spreadRotation * shootDirection;

        // Request server to spawn bullet
        ShootServerRpc(firePoint.position, finalDirection);

        // Spawn muzzle flash locally for immediate feedback
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, Quaternion.identity);
            Destroy(flash, muzzleFlashDuration);
        }

        bulletsLeft--;
        bulletsShot--;

        Invoke(nameof(ResetShot), timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0)
        {
            Invoke(nameof(Shoot), timeBetweenShooting);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPosition, Vector2 direction)
    {
        // Spawn bullet on server
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
         

        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        netObj.Spawn();

        // Initialize bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction, bulletSpeed, damage, OwnerClientId);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void Reload()
    {
        reloading = true;
        Invoke(nameof(ReloadFinished), reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void UpdateUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{bulletsLeft} / {magazineSize}";
        }
    }
}