
using UnityEditor;
using UnityEngine;

public class CreateWeaponPrefab
{
    [MenuItem("Tools/Create Weapon Prefab")]
    static void CreatePrefab()
    {
        // Create a new game object
        GameObject weaponGO = new GameObject("Weapon");

        // Add a SpriteRenderer
        SpriteRenderer sr = weaponGO.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Square.png");

        // Add a BoxCollider2D
        weaponGO.AddComponent<BoxCollider2D>();

        // Add a Rigidbody2D
        Rigidbody2D rb = weaponGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        // Add the Weapon script
        weaponGO.AddComponent<Weapon>();

        // Add the GunSystem script
        GunSystem gunSystem = weaponGO.AddComponent<GunSystem>();

        // Create the prefab
        PrefabUtility.SaveAsPrefabAsset(weaponGO, "Assets/Runtime/Prefabs/Weapon.prefab");

        // Destroy the temporary game object
        GameObject.DestroyImmediate(weaponGO);
    }
}
