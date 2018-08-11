using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Camera playerCamera;

    public GameObject weaponTip;
    public GameObject bullet;
    public GameObject turret;
    public Text nameTag;

    #region Xeon Properties

    private string playerName = "";

    private float speed = 12.0f;

    private float fireLast = 0.0f;
    private float fireSpeed = 0.2f;

    private float healthMax = 100.0f;
    private float health = 100.0f;

    #endregion

    private Vector3 cameraOffset;

    private Rigidbody2D rb2d;       //Store a reference to the Rigidbody2D component required to use 2D Physics.

    private bool isClient = false;
    private Client client;
    private int connectionID;

    // Use this for initialization
    void Start()
    {
        //Get and store a reference to the Rigidbody2D component so that we can access it.
        rb2d = GetComponent<Rigidbody2D>();
    }

    #region 

    public void heal(float amount)
    {
        // Add amount to health
        health += amount;

        // Clamp it down if it went over the max
        if (health > healthMax) { health = healthMax; }
    }

    public void takeDamage(int attackerID, float amount)
    {
        Debug.Log(nameTag.text + " taking " + amount.ToString() + " damage from player " + attackerID);

        // Subtract amount from health
        health -= amount;

        // If health is less than or equal to 0
        if (health <= 0)
        {
            // Set health to 0
            health = 0;

            // Call our death function
            death(attackerID);
        }
    }

    public void death(int killerID)
    {
        // Player a death animation


        // Destroy this gameObject
        Destroy(gameObject);

        // Tell the server that we died


    }

    #endregion

    #region Client Server Setup

    public void setIsClient(bool isClient, Client client)
    {
        // Check the client to isClient
        this.isClient = isClient;

        // Save the client passed so we can send it messages
        this.client = client;

        // Get the camera for the player
        playerCamera = Camera.main;

        // Start the camera with an initial offset
        cameraOffset = playerCamera.transform.position;
    }

    public void setPlayerID(int id) { connectionID = id;}

    public void setName(string name)
    {
        playerName = name;

        nameTag.text = name;
    }

    #endregion

    #region Updaters

    private void Update()
    {
        // If we are not the client, stop
        if (!isClient)
        {
            return;
        }

        // Handle camera movement
        handleCamera();
    }

    //FixedUpdate is called at a fixed interval and is independent of frame rate. Put physics code here.
    void FixedUpdate()
    {
        // If we are not the client, stop
        if (!isClient)
        {
            return;
        }

        // Handle rotation before everything
        handleRotation();

        //print("X: " + transform.rotation.x + " Y: " + transform.rotation.y + " Z: " + transform.rotation.z + " W: " + transform.rotation.w);

        // Handle movement based on input
        handleMovement();

        // Handle attack based on player input
        handleAttack();
    }

    #endregion

    #region Getters

    public int getPlayerConnID() { return connectionID; }

    #endregion

    #region Player Handlers

    private void handleCamera()
    {
        playerCamera.transform.position = transform.position + cameraOffset;
    }

    private void handleRotation()
    {
        // Get the mouse from the input
        Vector3 mouseScreen = Input.mousePosition;

        // Convert it to world point
        Vector3 mouse = Camera.main.ScreenToWorldPoint(mouseScreen);

        // Calculate rotation to point
        turret.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(mouse.y - transform.position.y, mouse.x - transform.position.x) * Mathf.Rad2Deg - 90);
    }

    private void handleMovement()
    {
        // Store the current horizontal input in the float moveHorizontal.
        float moveHorizontal = Input.GetAxis("Horizontal");

        // Store the current vertical input in the float moveVertical.
        float moveVertical = Input.GetAxis("Vertical");

        // Use the two store floats to create a new Vector2 variable movement.
        Vector2 movement = new Vector2(moveHorizontal, moveVertical);

        // Call the AddForce function of our Rigidbody2D rb2d supplying movement multiplied by speed to move our player.
        rb2d.AddForce(movement * speed);
    }

    private void handleAttack()
    {
        // Update our last fire cooldown
        fireLast += Time.deltaTime;

        // Check if we want to and can fire
        if (Input.GetButton("Fire1") && fireLast > fireSpeed)
        {
            // Reset fireLast
            fireLast = 0f;

            // Calculate the our position with current velocity
            Vector3 position = weaponTip.transform.position;
            position.x += rb2d.velocity.x / 23.0f;
            position.y += rb2d.velocity.y / 23.0f;

            // Tell the server to spawn a bullet
            client.PlayerFire(PrefabID.genericBulletID, position, weaponTip.transform.rotation);
        }
    }

    #endregion
}
