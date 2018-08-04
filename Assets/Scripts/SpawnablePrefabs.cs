using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PrefabID
{
    public const string genericBulletID = "genericBullet";
}

public class SpawnablePrefabs : MonoBehaviour 
{

    [Header("Player Prefab")]

    public GameObject playerPrefab;

    [Header("Bullets")]

    public GameObject genericBullet;


    public GameObject getPrefabWithID(string ID)
    {
        switch (ID)
        {
            case PrefabID.genericBulletID:
                return genericBullet;
        }

        return genericBullet;
    }

}
