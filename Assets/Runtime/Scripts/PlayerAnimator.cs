using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Unity.Netcode;

public class PlayerAnimator : NetworkBehaviour
{
    private PlayerMovement mov;
    [SerializeField] private Animator anim;
    private SpriteRenderer spriteRend;
    private DemoManager demoManager;

    [Header("Movement Tilt")]
    [SerializeField] private float maxTilt;
    [SerializeField][Range(0, 1)] private float tiltSpeed;

    [Header("Particle FX")]
    [SerializeField] private GameObject jumpFX;
    [SerializeField] private GameObject landFX;
    [SerializeField] private Color foregroundColor;

    private ParticleSystem _jumpParticle;
    private ParticleSystem _landParticle;

    public bool startedJumping { private get; set; }
    public bool justLanded { private get; set; }
    public float currentVelY;

    private void Start()
    {
        mov = GetComponent<PlayerMovement>();
        spriteRend = GetComponentInChildren<SpriteRenderer>();
        anim = spriteRend.GetComponent<Animator>();
        demoManager = FindObjectOfType<DemoManager>();

        _jumpParticle = jumpFX.GetComponent<ParticleSystem>();
        _landParticle = landFX.GetComponent<ParticleSystem>();
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        #region Tilt
        float tiltProgress;
        int mult = -1;

        if (mov.IsSliding)
        {
            tiltProgress = 0.25f;
        }
        else
        {
            tiltProgress = Mathf.InverseLerp(-mov.Data.runMaxSpeed, mov.Data.runMaxSpeed, mov.RB.linearVelocity.x);
            mult = (mov.IsFacingRight) ? 1 : -1;
        }

        float newRot = ((tiltProgress * maxTilt * 2) - maxTilt);
        float rot = Mathf.LerpAngle(spriteRend.transform.localRotation.eulerAngles.z * mult, newRot, tiltSpeed);
        spriteRend.transform.localRotation = Quaternion.Euler(0, 0, rot * mult);
        #endregion

        CheckAnimationState();

        ParticleSystem.MainModule jumpPSettings = _jumpParticle.main;
        jumpPSettings.startColor = new ParticleSystem.MinMaxGradient(foregroundColor);
        ParticleSystem.MainModule landPSettings = _landParticle.main;
        landPSettings.startColor = new ParticleSystem.MinMaxGradient(foregroundColor);
    }

    private void CheckAnimationState()
    {
        if (startedJumping)
        {
            // Trigger animation and effect on all clients
            TriggerJumpServerRpc();
            startedJumping = false;
            return;
        }

        if (justLanded)
        {
            // Trigger animation and effect on all clients
            TriggerLandServerRpc();
            justLanded = false;
            return;
        }

        anim.SetFloat("Vel Y", mov.RB.linearVelocity.y);
    }

    [ServerRpc]
    private void TriggerJumpServerRpc()
    {
        // Tell all clients to play jump effect
        TriggerJumpClientRpc();
    }

    [ClientRpc]
    private void TriggerJumpClientRpc()
    {
        // Play animation
        anim.SetTrigger("Jump");

        // Spawn particle effect
        Vector3 spawnPos = transform.position - (Vector3.up * transform.localScale.y / 2);
        GameObject obj = Instantiate(jumpFX, spawnPos, Quaternion.Euler(-90, 0, 0));
        Destroy(obj, 1);
    }

    [ServerRpc]
    private void TriggerLandServerRpc()
    {
        // Tell all clients to play land effect
        TriggerLandClientRpc();
    }

    [ClientRpc]
    private void TriggerLandClientRpc()
    {
        // Play animation
        anim.SetTrigger("Land");

        // Spawn particle effect
        Vector3 spawnPos = transform.position - (Vector3.up * transform.localScale.y / 1.5f);
        GameObject obj = Instantiate(landFX, spawnPos, Quaternion.Euler(-90, 0, 0));
        Destroy(obj, 1);
    }
}