using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour 
{

    public float bulletSpeed;

    private int playerID;

	// Use this for initialization
	void Start () 
    {
        // Set his velocity
        Vector3 velocity3D = transform.up * bulletSpeed;
        GetComponent<Rigidbody2D>().velocity = velocity3D;


        // Destroy ourselves after a few seconds
        Destroy(this.gameObject, 5.0f);
	}

    public void setPlayerID(int ID) { playerID = ID; }
}
