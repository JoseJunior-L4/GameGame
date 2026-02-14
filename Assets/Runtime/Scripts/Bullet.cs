using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
public class Bullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    public float lifetime = 5f;
    public LayerMask hitLayers;
    public GameObject hitEffectPrefab;

    private Vector2 direction;
    private float speed;
    private int damage;
    private ulong ownerClientId;
    private Rigidbody2D rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 dir, float spd, int dmg, ulong owner)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        ownerClientId = owner;

        // Set velocity
        rb.linearVelocity = direction * speed;

        // Rotate bullet to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || hasHit) return;

        // Don't hit the owner
        NetworkObject netObj = collision.GetComponent<NetworkObject>();
        if (netObj != null && netObj.OwnerClientId == ownerClientId)
            return;

        hasHit = true;

        // Check if we hit a damageable object
        IDamageable damageable = collision.GetComponent<IDamageable>();
        print(collision.gameObject.name);

        if (damageable != null)
        {
            damageable.TakeDamage(damage, (collision.transform.position - transform.position).normalized);
        }

        // Spawn hit effect
        SpawnHitEffectClientRpc(transform.position);

        // Destroy bullet
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }

    [ClientRpc]
    private void SpawnHitEffectClientRpc(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
}
public interface IDamageable
{
    void TakeDamage(int damage, Vector2 hitDirection);
}