using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class WeaponSpawner : NetworkBehaviour
{
    public GameObject weaponPrefab;
    public Transform[] spawnPoints;

    private List<GameObject> spawnedWeapons = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnWeapons();
        }
    }
     
    public void SpawnWeapons()
    {
        if (!IsServer) return;

        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject weapon = Instantiate(weaponPrefab, spawnPoint.position, spawnPoint.rotation);
            weapon.GetComponent<NetworkObject>().Spawn();
            spawnedWeapons.Add(weapon);
        }
    }
     
    public int GetActiveWeaponCount()
    {
        // Remove null entries (destroyed weapons)
        spawnedWeapons.RemoveAll(weapon => weapon == null);
        return spawnedWeapons.Count;
    }

    
    public void ClearWeapons()
    {
        if (!IsServer) return;

        foreach (GameObject weapon in spawnedWeapons)
        {
            if (weapon != null)
            {
                NetworkObject networkObject = weapon.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
                Destroy(weapon);
            }
        }

        spawnedWeapons.Clear();
    }
}