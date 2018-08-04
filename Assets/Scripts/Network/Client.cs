using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player
{
    public string playerName;
    public GameObject avatar;
    public int connectionID;

}

public class Client : MonoBehaviour 
{
    private const int MAX_CONNECTIONS = 100;

    private int port = 5701;

    private int hostID;
    private int webHostID;

    private int reliableChannel;
    private int unreliableChannel;

    private int clientID;
    private int connectionID;

    private bool isConnected = false;
    private bool isStarted = false;
    private float connectionTime;
    private byte error;

    private string playerName;

    public Dictionary<int, Player> players = new Dictionary<int, Player>();

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
        hostID = NetworkTransport.AddHost(topo, 0);
        connectionID = NetworkTransport.Connect(hostID, "127.0.0.1", port, 0, out error);

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
                    case "ASKNAME":
                        OnAskName(splitData);
                        break;
                    case "CONN":
                        SpawnPlayer(splitData[1], int.Parse(splitData[2]));
                        break;
                    case "DC":
                        PlayerDisconnected(connectionID);
                        break;
                    case "ASKPOSITION":
                        OnAskPosition(splitData);
                        break;
                    case NetworkingConstants.PLAYER_FIRE:
                        OnPlayerFire(splitData);
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

    private void OnAskName(string[] data)
    {
        // Set our own Id
        clientID = int.Parse(data[1]);

        // Send our name to the server
        Send("NAMEIS|" + playerName, reliableChannel);

        // Create all other players
        for (int i = 2; i < data.Length - 1; i++)
        {
            string[] playerData = data[i].Split('%');
            SpawnPlayer(playerData[0], int.Parse(playerData[1]));
        }
    }
    private void OnAskPosition(string[] data)
    {
        // Loop through all of the data
        for (int i = 1; i < data.Length; i++)
        {
            // Data structure: ConnectionID%posX%posY%velX%velY%rotZ%rotW
            string[] playerData = data[i].Split('%');

            // Prevent the server from updating us
            if (this.clientID != int.Parse(playerData[0]))
            {
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
                GameObject playerAvatar = players[int.Parse(playerData[0])].avatar;
                playerAvatar.transform.position = position;
                playerAvatar.transform.rotation = rotation;
                playerAvatar.GetComponent<Rigidbody2D>().velocity = velocity;
            }
        }

        // If we have been spawned
        if (players.ContainsKey(this.clientID))
        {
            // Send our position to the server
            Vector3 myPosition = players[this.clientID].avatar.transform.position;
            Vector2 myVelocity = players[this.clientID].avatar.GetComponent<Rigidbody2D>().velocity;
            Quaternion myRotation = players[this.clientID].avatar.transform.rotation;
            // DATA STRUCTURE: posX|posY|velX|velY|rotZ|rotW
            string positionM = NetworkingConstants.MY_POSITION + "|" + myPosition.x.ToString() + "|" + myPosition.y.ToString() + "|" 
                                                         + myVelocity.x.ToString() + "|" + myVelocity.y.ToString() + "|"
                                                         + myRotation.z.ToString() + "|" + myRotation.w.ToString();
            Send(positionM, unreliableChannel);
        }
    }
    private void OnPlayerFire(string[] data)
    {
        // DATA STRUCTURE: TAG|connID|bulletID%posX%posY%rotDegree
        // Parse the data
        string[] bulletData = data[2].Split('%');

        // Get the position of the bullet
        Vector3 position = Vector3.zero;
        position.x = float.Parse(bulletData[1]);
        position.y = float.Parse(bulletData[2]);

        // Get the rotation of the bullet
        Quaternion rotation = Quaternion.Euler(0, 0, float.Parse(bulletData[3]));

        // Create the bullet and give it its rotation and position
        GameObject newBullet = Instantiate(this.GetComponent<SpawnablePrefabs>().getPrefabWithID(bulletData[0]), 
                                           position, rotation);
        newBullet.GetComponent<Bullet>().setPlayerID(int.Parse(data[1]));

    }

    public void PlayerFire(string bulletID, Vector2 position, Quaternion rotation)
    {
        // Prep the message
        // DATA STRUCTURE: bulletID%posX%poxY%rotDegree
        string fireMessage = NetworkingConstants.PLAYER_FIRE + "|" + bulletID + "%" 
            + position.x.ToString() + "%" + position.y.ToString() + "%"
                      + rotation.eulerAngles.z.ToString();

        Send(fireMessage, reliableChannel);
    }

    private void SpawnPlayer(string newPlayerName, int connID)
    {
        GameObject playerObject = Instantiate(GetComponent<SpawnablePrefabs>().playerPrefab) as GameObject;

        // If this is our player
        if (connID == clientID)
        {
            // Remove Canvas
            GameObject.Find("Canvas").SetActive(false);

            // Add mobility
            playerObject.GetComponent<PlayerController>().setIsClient(true, this);

            // Start ourselves
            isStarted = true;
        }

        Player p = new Player();
        p.avatar = playerObject;
        p.playerName = newPlayerName;
        p.connectionID = connID;
        players.Add(connID, p);

    }

    private void PlayerDisconnected(int connID)
    {
        // Destroy the player avatar
        Destroy(players[connID].avatar);

        // Remove the player from our dictionary
        players.Remove(connID);
    }

    private void Send(string message, int channgelID)
    {
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, channgelID, msg, message.Length * sizeof(char), out error);
    }

}
