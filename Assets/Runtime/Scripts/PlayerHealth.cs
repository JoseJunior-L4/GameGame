using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 10;

    [Header("Damage Flash")]
    public float flashDuration = 0.2f;
    public Color flashColor = Color.red;
    public int flashCount = 1;

    [Header("Death Physics")]
    public float deathForce = 8f;
    public float deathTorque = 6f;
    public ForceMode2D forceMode = ForceMode2D.Impulse;

    [Header("Behaviour Control")]
    public Behaviour[] disableOnDeath;
    public Behaviour[] enableOnDeath;

    private NetworkVariable<int> currentHealth = new(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private Rigidbody2D rb;

    //private NetworkedPlayer networkedPlayer;
    private SmashCameraController cameraController;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to camera on spawn
        cameraController = FindObjectOfType<SmashCameraController>();
        if (cameraController != null)
        {
            cameraController.SubscribePlayer(transform);
        }
        else
        {
            Debug.LogWarning("SmashCameraController not found! Player won't be tracked by camera.");
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (!IsServer || isDead.Value)
            return;

        currentHealth.Value -= damage;
        TriggerDamageFlashClientRpc();

        if (currentHealth.Value <= 0)
        {
            Die(hitDirection);
        }
    }

    private void Die(Vector2 hitDirection)
    {
        if (!IsServer)
            return;

        // Notify network first - this will unsubscribe on all clients
        //if (networkedPlayer != null && IsOwner)
        //{
        //    networkedPlayer.SendDeathServerRpc();
        //}


        isDead.Value = true;
        if (cameraController != null)
        {
            cameraController.UnsubscribePlayer(transform);
        }
        StopDamageFlashClientRpc();

        DieClientRpc(hitDirection.normalized);

        Debug.Log($"Player {OwnerClientId} died");
    }

    [ClientRpc]
    private void DieClientRpc(Vector2 hitDirection)
    {
        HandleBehaviourToggles();
        ApplyDeathPhysics(hitDirection);
    }

    private void HandleBehaviourToggles()
    {
        foreach (var b in disableOnDeath)
        {
            if (b != null)
                b.enabled = false;
        }

        foreach (var b in enableOnDeath)
        {
            if (b != null)
                b.enabled = true;
        }
    }

    private void ApplyDeathPhysics(Vector2 hitDirection)
    {
        if (rb == null)
            return;

        // Ensure physics is active
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Push BACK from the shot
        Vector2 forceDir = -hitDirection;

        rb.AddForce(forceDir * deathForce, forceMode);

        // Rotate based on which side was hit
        float torqueSign = hitDirection.x >= 0f ? -1f : 1f;
        rb.AddTorque(deathTorque * torqueSign, forceMode);
    }

    [ClientRpc]
    private void TriggerDamageFlashClientRpc()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration * 0.5f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        spriteRenderer.color = originalColor;
    }

    [ClientRpc]
    private void StopDamageFlashClientRpc()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    public int GetCurrentHealth() => currentHealth.Value;
    public int GetMaxHealth() => maxHealth;
}
