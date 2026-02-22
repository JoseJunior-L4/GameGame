
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(GunSystem))]
public class Weapon : NetworkBehaviour
{
    public GunSystem gunSystem;
    // This variable is used to sync whether the weapon is on the ground or not
    public NetworkVariable<bool> onGround = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        gunSystem = GetComponent<GunSystem>();
    }
}
