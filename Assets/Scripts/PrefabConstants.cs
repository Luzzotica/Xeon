using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PrefabID
{
    public const string genericBulletID = "genericBullet";
}

public class PrefabConstants : MonoBehaviour
{

    [Header("Player Prefab")]

    public GameObject playerPrefab;

    [Header("Bullets")]

    public GameObject genericBullet;

    [Header("Damage Values")]

    public float genericBulletDamage;


    public GameObject getPrefabWithID(string ID)
    {
        switch (ID)
        {
            case PrefabID.genericBulletID:
                return genericBullet;
        }

        return genericBullet;
    }

    public float getDamageWithID(string ID)
    {
        switch (ID)
        {
            case PrefabID.genericBulletID:
                return genericBulletDamage;
        }

        return genericBulletDamage;
    }

}
