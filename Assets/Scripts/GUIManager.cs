using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIManager : MonoBehaviour
{
    [Header("Global Controllers")]

    public GameManager gameManager;
    public Client client;

    [Header("GUIs")]

    public GameObject signIn;
    public GameObject playerHUD;
    public Image healthBar;

    public Text countdownTimer;

    Color healthMaxColor = new Color(0.01f, 0.88f, 0f, 1.0f);
    Color healthZeroColor = new Color(0.99f, 0.055f, 0f, 1.0f);

    private float deathTimerStart = 10.0f;
    private float deathTimer = 10.0f;

    public void playerSpawned()
    {
        // Show the normal HUD
        playerHUD.SetActive(true);

        // Hide the countdown timer
        hideCountdownTimer();
    }

    public void playerConnected()
    {
        // Hide the signin
        signIn.SetActive(false);

        // Show the countdown timer
        countdownTimer.gameObject.SetActive(true);
    }

    public void playerDied()
    {
        // Hide the normal HUD and show the countdownTimer
        playerHUD.SetActive(false);

        // Start the countdown timer, this shows the timer as well
        startCountdown();
    }

    public void setHealth(float healthPercent)
    {
        // Set the scale of the health bar
        healthBar.rectTransform.localScale = new Vector3(1, healthPercent, 1);

        // Lerp to red as the health goes down
        healthBar.color = Color.Lerp(healthZeroColor, healthMaxColor, healthPercent);

        //print(healthPercent);
    }

    #region Countdown and Spawning

    public bool canSpawn()
    {
        // If the timer is <= 0 , the answer is YES
        return deathTimer <= 0 && countdownTimer.gameObject.activeSelf;
    }

    public void startCountdown()
    {
        // Setup the timer
        deathTimer = deathTimerStart;

        // Show the countdownTimer
        countdownTimer.gameObject.SetActive(true);
    }

    public void hideCountdownTimer()
    {
        // Hide the countdownTimer
        countdownTimer.gameObject.SetActive(false);
    }

    private void Update()
    {
        // If the timer is greater than 0, subtract delta.time from it
        if (deathTimer > 0f)
        {
            deathTimer -= Time.deltaTime;

            // Update the text of the countdown timer
            countdownTimer.text = "Respawn in " + (int)deathTimer + " seconds";
        }
        else
        {
            // Update the text of the countdown timer
            countdownTimer.text = "Left click to respawn";
        }

        // Check if we want to respawn!
        handleRespawn();
    }

    private void handleRespawn()
    {
        // Check if we want to and can respawn
        if (Input.GetMouseButtonDown(0) && canSpawn())
        {
            // Hide the UI
            hideCountdownTimer();

            // If we can, spawn ourselves!
            client.PlayerAskSpawn();
        }
        // Otherwise, speed up the timer on mouse click, if we can
        else if (Input.GetMouseButtonDown(0) && deathTimer > 3f)
        {
            deathTimer -= 1f;
        }
    }

    #endregion

}
