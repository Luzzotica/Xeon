using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player
{
    public PlayerController controller;
    public string playerName;
}

public class Client : MonoBehaviour 
{
    public GameObject gameManager;
    public GUIManager guiManager;

    private const int MAX_CONNECTIONS = 100;

    private int clientPort = 5700;
    private int serverPort = 5701;

    private int hostID;
    private int webHostID;

    private int reliableChannel;
    private int unreliableChannel;

    // The ID that we have in the server, given to us by the server on connection
    private int clientID;
    // The ID that have on our end. Calculated when we connect with the server
    private int connectionID;

    private bool isConnected = false;
    private bool isStarted = false;
    private float connectionTime;
    private byte error;

    private string playerName;

    public Dictionary<int, Player> players = new Dictionary<int, Player>();
    public Dictionary<int, GameObject> bullets = new Dictionary<int, GameObject>();

	public void Connect()
    {
        // Check if the player has a name
        string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if (pName == "")
        {
            // If he doesn't, stop
            Debug.Log("You must have a name!");
            return;
        }

        // Setup the player name
        playerName = pName;

        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        // Setup the channels
        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);

        // Create a host topology
        HostTopology topo = new HostTopology(config, MAX_CONNECTIONS);

        // Add hostID
        hostID = NetworkTransport.AddHost(topo, clientPort);
        connectionID = NetworkTransport.Connect(hostID, "10.37.20.73", serverPort, 0, out error);

        // Start the server
        connectionTime = Time.time;
        isConnected = true;
    }

    private void Update()
    {
        if (!isConnected)
        {
            return;
        }

        int recHostID;
        int updateConnectionID;
        int channelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte updateError;
        NetworkEventType recData = NetworkTransport.Receive(out recHostID, out updateConnectionID, out channelID, recBuffer, bufferSize, out dataSize, out updateError);
        switch (recData)
        {
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                string[] splitData = msg.Split('|');
                Debug.Log("Recieving: " + msg);

                switch(splitData[0])
                {
                    case NetworkingConstants.ASK_NAME:
                        OnAskName(splitData);
                        break;
                    case NetworkingConstants.CONNECTION:
                        // DATA STRUCTURE: TAG|newPlayerName%connID
                        AddPlayer(splitData[1].Split('%'));
                        break;
                    case "DC":
                        PlayerDisconnected(int.Parse(splitData[1]));
                        break;
                    case NetworkingConstants.ASK_POSITION:
                        OnAskPosition(splitData);
                        break;
                    case NetworkingConstants.SPAWN_PLAYER:
                        SpawnPlayer(splitData);
                        break;
                    case NetworkingConstants.PLAYER_FIRE:
                        OnPlayerFire(splitData);
                        break;
                    case NetworkingConstants.PLAYER_HIT:
                        OnPlayerHit(splitData);
                        break;
                    case NetworkingConstants.PLAYER_DIED:
                        OnPlayerDeath(splitData);
                        break;
                    default:
                        Debug.Log("Invalid Message: " + msg);
                        break;
                }

                break;
            case NetworkEventType.BroadcastEvent:

                break;
        }
    }

    #region Server Message Action

    private void OnAskName(string[] data)
    {
        // DATA STRUCTURE: ASKNAME|connID|playerName%playerConnID%posX%poxY|playerName%playerConnID%posX%poxY|...
        // Set our own Id
        clientID = int.Parse(data[1]);

        // Send our name to the server
        Send(NetworkingConstants.NAME_IS + "|" + playerName, reliableChannel);

        // Create all other players
        for (int i = 2; i < data.Length - 1; i++)
        {
            string[] playerData = data[i].Split('%');

            AddPlayer(playerData);
        }
    }
    private void OnAskPosition(string[] data)
    {
        // Loop through all of the data
        for (int i = 1; i < data.Length; i++)
        {
            // Data structure: ConnectionID%posX%posY%velX%velY%rotZ%rotW
            string[] playerData = data[i].Split('%');

            // Get the playerConnID from the player data
            int playerConnID = int.Parse(playerData[0]);

            // Make sure we have spawned in the player first and that he is not dead
            if (!players.ContainsKey(playerConnID))
            {
                // If it doesn't contain the key, then go to the next one
                continue;
            }
            else if (players.ContainsKey(playerConnID) && players[playerConnID].controller == null)
            {
                continue;
            }

            // Prevent the server from updating us
            if (this.clientID != playerConnID)
            {
                // Get the player avatar
                GameObject playerAvatar = players[playerConnID].controller.gameObject;

                // If the player avatar is null, stop
                if (playerAvatar == null)
                {
                    continue;
                }

                // Parse through data
                /// Get position
                Vector3 position = Vector3.zero;
                position.x = float.Parse(playerData[1]);
                position.y = float.Parse(playerData[2]);

                // Get speed
                Vector2 velocity = Vector2.zero;
                velocity.x = float.Parse(playerData[3]);
                velocity.y = float.Parse(playerData[4]);

                // Get rotation
                Quaternion rotation = Quaternion.identity;
                rotation.z = float.Parse(playerData[5]);
                rotation.w = float.Parse(playerData[6]);

                // Update the player avatar with data found

                playerAvatar.transform.position = position;
                playerAvatar.GetComponent<PlayerController>().setNewRotation(rotation);
                playerAvatar.GetComponent<Rigidbody2D>().velocity = velocity;
            }
        }

        // If we have been spawned
        if (players.ContainsKey(this.clientID))
        {
            // Declare message variable
            string positionM = "";

            // Make sure the player controller exists
            if (players[this.clientID].controller == null)
            {
                // If it doesn't, tell the server so
                positionM = "NULL";
                return;
            }
            // If the player controller exists, send our position and stuff!
            else
            {
                // Send our position to the server
                Vector3 myPosition = players[this.clientID].controller.gameObject.transform.position;
                Vector2 myVelocity = players[this.clientID].controller.gameObject.GetComponent<Rigidbody2D>().velocity;
                Quaternion myRotation = players[this.clientID].controller.turret.transform.rotation;
                // DATA STRUCTURE: posX|posY|velX|velY|rotZ|rotW
                positionM = NetworkingConstants.MY_POSITION + "|" + myPosition.x.ToString() + "|" + myPosition.y.ToString() + "|"
                                                             + myVelocity.x.ToString() + "|" + myVelocity.y.ToString() + "|"
                                                             + myRotation.z.ToString() + "|" + myRotation.w.ToString();
            }

            Send(positionM, unreliableChannel);
        }
    }
    private void OnPlayerFire(string[] data)
    {
        // DATA STRUCTURE: TAG|connID|bulletServerID%bulletID%posX%posY%rotDegree
        // Parse the data
        string[] bulletData = data[2].Split('%');

        // Get the position of the bullet
        Vector3 position = Vector3.zero;
        position.x = float.Parse(bulletData[2]);
        position.y = float.Parse(bulletData[3]);

        // Get the rotation of the bullet
        Quaternion rotation = Quaternion.Euler(0, 0, float.Parse(bulletData[4]));

        // Create the bullet and give it its rotation and position
        GameObject newBullet = Instantiate(this.GetComponent<PrefabConstants>().getPrefabWithID(bulletData[1]), 
                                           position, rotation);

        // Get the bullet component
        Bullet bulletComponent = newBullet.GetComponent<Bullet>();

        // Get the playerID
        int bulletPlayerID = int.Parse(data[1]);

        // Set the player ID of the bullet
        bulletComponent.setPlayerID(bulletPlayerID);
        // Tell the bullet component if this is the clients bullet or not. If bulletPlayerID == our connection ID, we were the ones that fired!
        bulletComponent.setIsClient(bulletPlayerID == this.clientID);

        // Add the bullet to our dictionary of bullets, each ID from the server will be unique, so this should work
        bullets[int.Parse(bulletData[0])] = newBullet;
    }
    private void OnPlayerHit(string[] data)
    {
        // DATA STRUCTURE: TAG|attackerID%bulletServerID%bulletDamage%targetID
        // Parse the data
        string[] hitData = data[1].Split('%');

        // Get the attackerID
        int attackerID = int.Parse(hitData[0]);

        // Get the damage to be done
        float damage = float.Parse(hitData[2]);
        // Get the targetID
        int targetID = int.Parse(hitData[3]);

        Debug.Log(players[targetID].playerName + " took " + damage.ToString() + " damage from player " + players[attackerID].playerName);

        // Get the controller
        PlayerController playerC = players[targetID].controller;

        // If it exists, do damage to it
        if (playerC != null)
        {
            // Get the Player of the targetID
            playerC.takeDamage(attackerID, damage);
        }

        // If the attackerID != to us
        if (attackerID != clientID)
        {
            // Get the bulletServerID
            int bulletServerID = int.Parse(hitData[1]);
            // Gameobject to destroy
            GameObject toDestroy;
            // Check if the bullet exists still
            if (bullets.TryGetValue(bulletServerID, out toDestroy))
            {
                // Destroy the bullet
                Destroy(toDestroy);
            }
        }
    }
    private void OnPlayerDeath(string[] data)
    {
        // DATA STRUCTURE: PLAYERDEATH|killerID%connectionID
        // Get the killerID and connID
        string[] deathData = data[1].Split('%');

        // Get the name of the killer and the connectionID
        string killerName = players[int.Parse(deathData[0])].playerName;
        string didDiedName = players[int.Parse(deathData[1])].playerName;

        // Get the controller for the person dying
        PlayerController toDie = players[int.Parse(deathData[1])].controller;

        // If the toDie is not null, we want to kill the player
        if (toDie != null)
        {
            // Destroy the avatar of the person who died
            players[int.Parse(deathData[1])].controller.death(-1);
        }


        // Send a message saying that the killer killed the killee

    }

    #endregion

    #region Client Action Execution

    public void PlayerFire(string bulletID, Vector2 position, Quaternion rotation)
    {
        // Prep the message
        // DATA STRUCTURE: PLAYER_FIRE|bulletID%posX%poxY%rotDegree
        string fireMessage = NetworkingConstants.PLAYER_FIRE + "|" + bulletID + "%" 
            + position.x.ToString() + "%" + position.y.ToString() + "%"
                      + rotation.eulerAngles.z.ToString();

        Send(fireMessage, reliableChannel);
    }
    public void PlayerHit(int bulletServerID, string bulletID, int targetID)
    {
        // Get the damage based on the bulletID
        float damage = GetComponent<PrefabConstants>().getDamageWithID(bulletID);

        //print("Bullet hit player " + targetID.ToString() + " for " + damage + " damage");

        // Prep the message
        // DATA STRUCTURE: PLAYER_HIT|bulletServerID%bulletDamage%targetID
        string hitMessage = NetworkingConstants.PLAYER_HIT + "|" + bulletServerID.ToString() + "%" + damage.ToString() + "%" + targetID;

        Send(hitMessage, reliableChannel);
    }
    public void PlayerDied(string killerID)
    {
        // Prep the message
        // DATA STRUCTURE: PLAYERDIED|killerID
        string deathMessage = NetworkingConstants.PLAYER_DIED + "|" + clientID.ToString() + "|" + killerID;

        // Send reliably
        Send(deathMessage, reliableChannel);
    }
    public void PlayerHeal()
    {
        
    }
    public void PlayerAskSpawn()
    {
        // Prep the message
        // DATA STRUCTURE: TAG
        string spawnM = NetworkingConstants.PLAYER_ASK_SPAWN;

        // Send reliably
        Send(spawnM, reliableChannel);
    }

    #endregion

    #region Player Spawning and Tracking

    private void SpawnPlayer(string[] data)
    {
        // DATA STRUCTURE: TAG|connID%posX%posY
        // Parse the data
        string[] spawnData = data[1].Split('%');

        int connID = int.Parse(spawnData[0]);
        float posX = float.Parse(spawnData[1]);
        float posY = float.Parse(spawnData[2]);

        // Prep the position and rotation
        Vector3 pos = new Vector3(posX, posY);
        Quaternion rot = Quaternion.identity;

        // Create a new avatar for the player
        players[connID].controller = SpawnPlayerAvatar(players[connID].playerName, connID, pos, rot);
    }
    private PlayerController SpawnPlayerAvatar(string newPlayerName, int connID, Vector3 pos, Quaternion rot)
    {
        // Create a new player prefab
        GameObject playerAvatar = Instantiate(GetComponent<PrefabConstants>().playerPrefab, pos, Quaternion.identity) as GameObject;

        //// Move the avatar to the specified point, and change his rotation
        //playerAvatar.transform.position = pos;

        // Get the controller component
        PlayerController playerC = playerAvatar.GetComponent<PlayerController>();
        // Set the initial rotation of the avatar
        playerC.turret.transform.rotation = rot;

        // Set the name of the tag on the controller
        playerC.setName(newPlayerName);
        // Set the connID of the player controller
        playerC.setPlayerID(connID);

        // Check if we are the client
        if (connID == clientID)
        {
            // If we are, set this playerC to be the client, this adds ->MOBILITY<- among other things
            playerC.setIsClient(true, this);
        }

        return playerC;
    }
    private void AddPlayer(string[] data)
    {
        // We can assume it was split beforehand
        // DATA STRUCTURE: playerName%playerConnID(%posX%poxY%rotZ%rotW) <-OPTIONAL

        // Parse the values
        string newPlayerName = data[0];
        int connID = int.Parse(data[1]);

        // Create a new Player, and set his properties
        Player p = new Player();
        p.playerName = newPlayerName;

        // If this is our player
        if (connID == clientID)
        {
            // Start ourselves
            isStarted = true;

            // Hide the UI
            guiManager.playerConnected();
        }

        // Make sure the dictionary doesn't have this key
        if (!players.ContainsKey(connID))
        {
            // Add the Player script to our dictionary
            players.Add(connID, p);
        }


        // If there are more than 2 vlaues in data
        if (data.Length > 2)
        {
            // Then we were given a position and rotation, parse them and make a player avatar with them
            float posX = float.Parse(data[2]);
            float posY = float.Parse(data[3]);
            float rotZ = float.Parse(data[4]);
            float rotW = float.Parse(data[5]);

            // Create our pos and rot
            Vector3 pos = new Vector3(posX, posY);
            Quaternion rot = new Quaternion(0, 0, rotZ, rotW);

            // Spawn an avatar for the player
            // Set the controller to be the player controller of the avatar we spawn
            p.controller = SpawnPlayerAvatar(newPlayerName, connID, pos, rot);
        }
    }

    private void PlayerDisconnected(int connID)
    {
        // If the controller and avatar still exist
        if (players[connID].controller != null)
        {
            // Destroy the player avatar
            Destroy(players[connID].controller.gameObject);
        }

        // Remove the player from our dictionary
        players.Remove(connID);
    }

    #endregion

    private void Send(string message, int channgelID)
    {
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, channgelID, msg, message.Length * sizeof(char), out error);
    }

}
