using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public float speed;

    public GameObject weaponTip;
    public GameObject bullet;

    private float fireLast = 0.0f;
    private float fireSpeed = 0.2f;

    private Vector3 cameraOffset;

    private Rigidbody2D rb2d;       //Store a reference to the Rigidbody2D component required to use 2D Physics.

    private bool isClient = false;

    // Use this for initialization
    void Start()
    {
        //Get and store a reference to the Rigidbody2D component so that we can access it.
        rb2d = GetComponent<Rigidbody2D>();
    }

    public void setIsClient(bool isClient)
    {
        // Change the client
        this.isClient = isClient;

        // Get the camera for the player
        playerCamera = Camera.main;

        // Start the camera with an initial offset
        cameraOffset = playerCamera.transform.position;
    }

    private void Update()
    {
        // If we are not the client, stop
        if (!isClient)
        {
            return;
        }

        // Handle camera movement
        handleCamera();

        // Handle attack based on player input
        handleAttack();
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
    }

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
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(mouse.y - transform.position.y, mouse.x - transform.position.x) * Mathf.Rad2Deg - 90);
    }

    private void handleMovement()
    {
        //Store the current horizontal input in the float moveHorizontal.
        float moveHorizontal = Input.GetAxis("Horizontal");

        //Store the current vertical input in the float moveVertical.
        float moveVertical = Input.GetAxis("Vertical");

        //Use the two store floats to create a new Vector2 variable movement.
        Vector2 movement = new Vector2(moveHorizontal, moveVertical);

        //Call the AddForce function of our Rigidbody2D rb2d supplying movement multiplied by speed to move our player.
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

            // Create our bullet
            GameObject newBullet = Instantiate(bullet, weaponTip.transform.position, weaponTip.transform.rotation);
        }
    }

    #endregion
}
