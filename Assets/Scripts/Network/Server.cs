using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class ServerClient
{
    public int connectionID;
    public string playerName;
    public Vector2 playerPosition;
    public Vector2 playerVelocity;
    public Quaternion playerRotation;

    public bool isAlive = false;
}

public class Server : MonoBehaviour 
{
    [Header("Spawn Points")]

    public GameObject spawnPoints;

    private const int MAX_CONNECTIONS = 100;

    private int port = 5701;

    private int hostID;
    private int webHostID;

    private int reliableChannel;
    private int unreliableChannel;

    private bool isStarted = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();

    private float lastMovementUpdate;
    private float movementUpdateRate = 0.05f;

    // The ID of the next bullet to spawn
    private int bulletIDCurrent = 0;

    private void Start()
    {
        Debug.Log("Starting Server");

        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        // Setup the channels
        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);

        // Create a host topology
        HostTopology topo = new HostTopology(config, MAX_CONNECTIONS);

        // Add hostID
        hostID = NetworkTransport.AddHost(topo, port);
        webHostID = NetworkTransport.AddWebsocketHost(topo, port);

        // Start the server
        isStarted = true;
    }

    private void Update()
    {
        if (!isStarted)
        {
            return;
        }

        int recHostID;
        int connectionID;
        int channelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte updateError;
        NetworkEventType recData = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, recBuffer, bufferSize, out dataSize, out updateError);
        switch (recData)
        {
            case NetworkEventType.Nothing: 
                
                break;
            case NetworkEventType.ConnectEvent:
                
                Debug.Log("Player " + connectionID + " has connected.");
                OnConnection(connectionID);
                break;
            case NetworkEventType.DataEvent:
                
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                string[] splitData = msg.Split('|');
                Debug.Log("Recieving: " + msg);

                switch (splitData[0])
                {
                    case NetworkingConstants.NAME_IS:
                        OnNameIs(connectionID, splitData[1]);
                        break;
                    case NetworkingConstants.MY_POSITION:
                        OnMyPosition(connectionID, splitData);
                        break;
                    case NetworkingConstants.PLAYER_ASK_SPAWN:
                        OnPlayerAskSpawn(connectionID);
                        break;
                    case NetworkingConstants.PLAYER_FIRE:
                        OnPlayerFire(connectionID, splitData);
                        break;
                    case NetworkingConstants.PLAYER_HIT:
                        OnPlayerHit(connectionID, splitData);
                        break;
                    case NetworkingConstants.PLAYER_DIED:
                        OnPlayerDeath(connectionID, splitData);
                        break;
                    default:
                        Debug.Log("Invalid Message: " + msg);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent: 
                
                Debug.Log("Player " + connectionID + " has disconnected.");
                OnDisconnection(connectionID);
                break;

            case NetworkEventType.BroadcastEvent:

                break;
        }

        // Ask player for their position
        if (Time.time - lastMovementUpdate > movementUpdateRate)
        {
            // Update the time
            lastMovementUpdate = Time.time;

            // Create a message
            string positionRequest = NetworkingConstants.ASK_POSITION + "|";
            foreach (ServerClient sc in clients)
            {
                // If they are alive, then pass their data
                if (sc.isAlive)
                {
                    // DATA STRUCTURE: ConnID%posX%posY%velX%velY%rotZ%rotW
                    positionRequest += sc.connectionID +
                                         "%" + sc.playerPosition.x + "%" + sc.playerPosition.y +
                                         "%" + sc.playerVelocity.x + "%" + sc.playerVelocity.y +
                                         "%" + sc.playerRotation.z + "%" + sc.playerRotation.w + "|";
                }
                // Otherwise, we don't have an avatar, and want to skip ourselves, no need to send positions
                else
                {
                    continue;
                }
            }
            positionRequest = positionRequest.Trim('|');

            // Ask position of clients
            Send(positionRequest, unreliableChannel, clients);
        }
    }

    private void OnConnection(int connID)
    {
        // Add him to a list
        ServerClient sc = new ServerClient();
        sc.connectionID = connID;
        sc.playerName = "TEMP";
        clients.Add(sc);

        // Tell the player his ID on the server for future communication
        // Request his name and send it to all the other players
        // DATA STRUCTURE: ASKNAME|connID|playerName%playerConnID(%posX%poxY%rotZ%rotW) <-OPTIONAL|playerName%playerConnID%posX%poxY%rotZ%rotW|...
        string msg = NetworkingConstants.ASK_NAME + "|" + connID + "|";
        foreach (ServerClient client in clients)
        {
            // Get the position and rotation
            Vector2 playerPos = client.playerPosition;
            Quaternion playerRot = client.playerRotation;

            // If tthe player isAlive, then pass the data
            if (client.isAlive)
            {
                msg += client.playerName + "%" + client.connectionID + "%" + playerPos.x.ToString() + "%" + playerPos.y.ToString() + 
                             "%" + playerRot.z.ToString() + "%" + playerRot.w.ToString() + "|";
            }
            // Otherwise, that player has no avatar, only pass his name, and connID
            else
            {
                msg += client.playerName + "%" + client.connectionID;
            }
        }

        msg = msg.Trim('|');

        // Send a message to the clients
        Send(msg, reliableChannel, connID);
    }
    private void OnDisconnection(int connID)
    {
        // Remove the player from our client list
        clients.Remove(clients.Find(x => x.connectionID == connID));

        // Tell everyone that that person has disconnected
        Send("DC|" + connID, reliableChannel, clients);
    }

    private void OnNameIs(int connID, string playerName)
    {
        // Link the name to the connection ID
        clients.Find(x => x.connectionID == connID).playerName = playerName;

        // Get a random spawn point
        //Vector3 randomSpawn = spawnPoints.GetComponent<SpawnPoints>().getRandomPoint();

        // DATA STRUCTURE: TAG|newPlayerName%connID
        // Send his name to all the other players, they will use this to spawn him
        Send(NetworkingConstants.CONNECTION + "|" + playerName + "%" + connID, reliableChannel, clients);
    }
    private void OnMyPosition(int connID, string[] splitData)
    {
        // Check if the split data size is greater than 2
        if (splitData.Length > 2)
        {
            // Parse the data from split data
            float posX = float.Parse(splitData[1]);
            float posY = float.Parse(splitData[2]);
            float velX = float.Parse(splitData[3]);
            float velY = float.Parse(splitData[4]);
            float rotZ = float.Parse(splitData[5]);
            float rotW = float.Parse(splitData[6]);

            // Find the server client
            ServerClient sc = clients.Find(c => c.connectionID == connID);

            // If we have data, then the player is alive
            sc.isAlive = true;

            // Update the position, velocity, and rotation we have stored
            sc.playerPosition = new Vector2(posX, posY);
            sc.playerVelocity = new Vector2(velX, velY);
            sc.playerRotation = new Quaternion(0, 0, rotZ, rotW);
        }
        // Otherwise, the player has no avatar, set position, velocity and rotation to null
        else
        {
            // Find the server client
            ServerClient sc = clients.Find(c => c.connectionID == connID);

            // If we don't have data, then the player is not alive
            sc.isAlive = false;

            // Don't update positions, no need
        }

    }
    private void OnPlayerFire(int connID, string[] splitData)
    {
        // DATA STRUCTURE: bulletID%posX%poxY%rotDegree
        // NEW DATASTRUCTE: PLAYER_FIRE|connID|bulletServerID%bulletID%posX%poxY%rotDegree
        string message = NetworkingConstants.PLAYER_FIRE + "|" + connID + "|" + bulletIDCurrent + "%" + splitData[1];

        // Increment the bulletIDCurrent so that each bullet has a different ID
        bulletIDCurrent++;
        // Make it loop at 100000 bullets, hope there won't be more than 100000 bullets in the map at any given moment
        bulletIDCurrent %= 100000;

        // Send the message to all the clients
        Send(message, reliableChannel, clients);
    }
    private void OnPlayerHit(int connID, string[] splitData)
    {
        // DATA STRUCTURE: bulletServerID%bulletDamage%targetID
        // NEW DATA STRUCTURE: PLAYER_HIT|attackerID%bulletServerID%bulletDamage%targetID
        // Prep message
        string hitMessage = NetworkingConstants.PLAYER_HIT + "|" + connID.ToString( ) + "%" + splitData[1];

        Send(hitMessage, reliableChannel, clients);
    }
    private void OnPlayerDeath(int connID, string[] splitData)
    {
        // DATA STRUCTURE: PLAYERDEATH|killerID
        // NEW DATA STRUCTURE: PLAYERDEATH|killerID%connID
        // Prep message
        string deathMessage = NetworkingConstants.PLAYER_DIED + "|" + splitData[1] + "%" + connID.ToString();

        // Send reliably to everyone
        Send(deathMessage, reliableChannel, clients);
    }
    private void OnPlayerAskSpawn(int connID)
    {
        // NEW DATA STRUCTURE: TAG|connID%posX%posY

        // Get a random spawn point
        Vector3 randomSpawn = spawnPoints.GetComponent<SpawnPoints>().getRandomPoint();

        // Prep the message
        string spawnM = NetworkingConstants.SPAWN_PLAYER + "|" + connID.ToString() + "%" + randomSpawn.x.ToString() + "%" + randomSpawn.y.ToString();

        // Send Reliably to all clients
        Send(spawnM, reliableChannel, clients);
    }

    private void Send(string message, int channelId, int connId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionID == connId));
        Send(message, channelId, c);
    }


    private void Send(string message, int channgelId, List<ServerClient> c)
    {
        //Debug.Log("Sending: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (ServerClient client in c)
        {
            NetworkTransport.Send(hostID, client.connectionID, channgelId, msg, message.Length * sizeof(char), out error);
        }
    }
}
