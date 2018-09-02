using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour 
{

    public float bulletSpeed;

    private int playerConnID = -1;
    private bool isClient = false;

    private int bulletServerID;

    private bool hasHit = false;

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
        // If we have hit someone...
        if (hasHit)
        {
            // Stop!
            return;
        }



        // If our playerConnID == -1, stop, we don't have an owner yet
        if (playerConnID == -1) { return; }


        // Try and get the Player component from the target
        PlayerController playerC = collision.gameObject.GetComponent<PlayerController>();

        // If p is not null and we are the client
        if (playerC != null)
        {
            //// If we aren't the client, stop
            //if (!isClient)
            //{ 
            //    Destroy(gameObject);
            //    return; 
            //}

            // Get the target connID
            int targetConnID = playerC.getPlayerConnID();

            // Case 1: We are the client and Target hit was another player
            if (isClient && targetConnID != playerConnID)
            {
                // Set has hit to true so we don't hit anyone else or hit multiple times
                hasHit = true;

                // Destroy the game object
                Destroy(gameObject);

                // Tell the client to send a message saying we hit someone
                GameObject.Find("Client").GetComponent<Client>().PlayerHit(bulletServerID, PrefabID.genericBulletID, targetConnID);
            }
            // Case 2: Target hit was another player, but we are not the client
            else if (!isClient && targetConnID != playerConnID)
            {
                // Destroy the bullet!
                Destroy(gameObject);
            }
            // Case 3: Target hit was ourselves, do nothing!
        }
        else
        {
            Destroy(gameObject);
        }



    }

    public void setPlayerID(int ID)
    {
        playerConnID = ID;
        //print("Owner ID: " + ID);
    }
    public void setIsClient(bool isClient)
    {
        this.isClient = isClient;
        //print(isClient);
    }
    public void setServerID(int ID) { bulletServerID = ID; }
}
