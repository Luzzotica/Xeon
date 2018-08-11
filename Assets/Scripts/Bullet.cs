using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour 
{

    public float bulletSpeed;

    private int playerConnID = -1;

	// Use this for initialization
	void Start () 
    {
        // Set his velocity
        Vector3 velocity3D = transform.up * bulletSpeed;
        GetComponent<Rigidbody2D>().velocity = velocity3D;


        // Destroy ourselves after a few seconds
        Destroy(this.gameObject, 5.0f);
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If our playerConnID == -1, stop, we don't have an owner yet
        if (playerConnID == -1) { return; }

        // Try and get the Player component from the target
        PlayerController playerC = collision.gameObject.GetComponent<PlayerController>();

        // If p is not null
        if (playerC != null)
        {
            // Get the target connID
            int targetConnID = playerC.getPlayerConnID();

            // Case 1: Target hit was another player
            if (targetConnID != playerConnID)
            {
                // Destroy ourselves
                Destroy(gameObject);

                // Tell the client to send a message saying we hit someone
                GameObject.Find("Client").GetComponent<Client>().PlayerHit(PrefabID.genericBulletID, playerConnID, targetConnID);

            }
            // Case 2: Target hit was ourselves, do nothing!
        }
        // Otherwise, we want to destroy ourselves
        else
        {
            Destroy(gameObject);
        }
        print("Bullet hit!");
    }

    public void setPlayerID(int ID) { playerConnID = ID; }
}
