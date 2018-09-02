using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoints : MonoBehaviour 
{

    [Header("Spawn Points")]

    public GameObject[] spawnPoints;

	public Vector3 getRandomPoint()
    {
        int rand = Random.Range(0, 10);

        return spawnPoints[rand].transform.position;
    }
}
