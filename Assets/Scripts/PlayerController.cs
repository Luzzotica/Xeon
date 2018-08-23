using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Camera playerCamera;

    [Header("Weapon")]

    public GameObject weaponTip;
    public GameObject bullet;
    public GameObject turret;

    [Header("Name Tag")]

    public Text nameTag;

    private GUIManager guiManager;
    private Image healthBar;

    #region Xeon Properties

    private string playerName = "";

    private float speed = 12.0f;

    private float fireLast = 0.0f;
    private float fireSpeed = 0.2f;

    private float healthMax = 100.0f;
    private float health = 100.0f;

    // Position LERP
    private Vector3 oldPosition = Vector3.zero;
    private Vector3 newPosition = Vector3.zero;
    private float positionLerpT = 0.2f;
    private float positionLerpSpeed = 0.2f;

    // Rotation LERP
    private Quaternion oldRotation = Quaternion.identity;
    private Quaternion newRotation = Quaternion.identity;
    private float rotationLerpT = 0.075f;
    private float rotationLerpSpeed = 0.07f;
    //private float rotationSpeedPerAngle = 200.0f;

    // Offset of the camera to add when you move the camera to the player
    private Vector3 cameraOffset = new Vector3(0, 0, -10);

    private Rigidbody2D rb2d;       //Store a reference to the Rigidbody2D component required to use 2D Physics.

    private bool isClient = false;
    private Client client;
    private int connectionID;

    #endregion

    // Use this for initialization
    void Start()
    {
        //Get and store a reference to the Rigidbody2D component so that we can access it.
        rb2d = GetComponent<Rigidbody2D>();
    }

    #region Physical Updates

    public void setNewPositionAndRotation(Vector3 newPosition, Quaternion newRotation)
    {
        setNewPosition(newPosition);
        setNewRotation(newRotation);
    }
    public void setNewPosition(Vector3 newPosition)
    {
        // Old position is where we are now
        oldPosition = transform.position;

        // New position is where we want to lerp to
        this.newPosition = newPosition;

        // Set the lerp T to 0 so we start lerping!
        positionLerpT = 0f;
    }
    public void setNewRotation(Quaternion newRotation)
    {
        // Old rotation is where we are now
        oldRotation = turret.transform.rotation;

        // New rotation is where we want to get to
        this.newRotation = newRotation;

        // Set the lerp T to 0 so it starts lerping from the beginning
        rotationLerpT = 0.0f;

        // Get angle difference
        //float angleDifference = Mathf.Abs(oldRotation.eulerAngles.z - newRotation.eulerAngles.z);

        // Set the lerp speed to be the angle difference divided by how fast we should lerp through those angles
        //rotationLerpSpeed = angleDifference / rotationSpeedPerAngle;
    }


    public void heal(float amount)
    {
        // Add amount to health
        health += amount;

        // Update the health bar
        updateHealthBar();

        // Clamp it down if it went over the max
        if (health > healthMax) { health = healthMax; }
    }

    public void takeDamage(int attackerID, float amount)
    {
        // Subtract amount from health
        health -= amount;

        // Update the health bar
        updateHealthBar();

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
        // TODO, Player death animation


        // Destroy this gameObject
        Destroy(gameObject);

        // If we are the client and the killerID isn't bogus
        if (isClient && killerID != -1)
        {
            // Tell the server that we died
            client.PlayerDied(killerID.ToString());

            // Tell the gui that we died
            guiManager.playerDied();
        }
    }

    public void updateHealthBar()
    {
        // If we are not the cient, don't update the health bar, no need
        if (!isClient)
        {
            return;
        }

        // Make sure we have pointers to the HUD health components

        // Get the current percentage of health
        float healthPercent = health / healthMax;

        // Clamp the percent between 0 and 1
        Mathf.Clamp(healthPercent, 0f, 1f);

        guiManager.setHealth(healthPercent);
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
        //cameraOffset = Vector3.zero; //playerCamera.transform.position;

        // Get the player HUD
        this.guiManager = client.guiManager;

        // Tell the gui we have been spawned
        guiManager.playerSpawned();

        // Make sure the health bar is visually correct
        updateHealthBar();
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

        // Heal
        //heal(0.5f);


        // Handle camera movement
        handleCamera();
    }

    //FixedUpdate is called at a fixed interval and is independent of frame rate. Put physics code here.
    void FixedUpdate()
    {
        // If we are not the client, stop
        if (!isClient)
        {
            // Handle a lerping (Smooth) rotation
            handleRotationLerp();
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

    #region Player Handlers

    private void handleCamera()
    {
        playerCamera.transform.position = transform.position + cameraOffset;
    }

    private void handlePositionLerp()
    {
        // Increment our T with delta time
        positionLerpT += Time.deltaTime;

        // If lerp t is less than the lerp speed, we want to keep lerping
        if (positionLerpT <= positionLerpSpeed)
        {
            // Get the progress
            float progress = positionLerpT / positionLerpSpeed;

            // Lerp based on progress
            transform.position = Vector3.Lerp(oldPosition, newPosition, progress);
        }
    }
    private void handleRotationLerp()
    {
        // Add deltaTime to our rotation lerp t
        rotationLerpT += Time.deltaTime;

        // If lerp t is less than lerp speed
        if (rotationLerpT <= rotationLerpSpeed)
        {
            // Get progress
            float progress = rotationLerpT / rotationLerpSpeed;

            // Set the lerp!
            turret.transform.rotation = Quaternion.Lerp(oldRotation, newRotation, progress);

        }
        // Otherwise we stop, we have lerped all the way!

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

            //takeDamage(0, 15f);

            // Calculate the our position with current velocity
            Vector3 position = weaponTip.transform.position;
            position.x += rb2d.velocity.x / 24.0f;
            position.y += rb2d.velocity.y / 24.0f;

            // Tell the server to spawn a bullet
            client.PlayerFire(PrefabID.genericBulletID, position, weaponTip.transform.rotation);
        }
    }

    #endregion

    #region Getters

    public int getPlayerConnID() { return connectionID; }

    #endregion
}
