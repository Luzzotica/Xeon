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
}

public class Server : MonoBehaviour 
{
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

    private void Start()
    {
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
                    case "NAMEIS":
                        OnNameIs(connectionID, splitData[1]);
                        break;
                    case "MYPOSITION":
                        OnMyPosition(connectionID, splitData);
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
            string positionRequest = "ASKPOSITION|";
            foreach (ServerClient sc in clients)
            {
                // DATA STRUCTURE: ConnID%posX%posY%velX%velY%rotZ%rotW
                positionRequest += sc.connectionID + 
                                     "%" + sc.playerPosition.x + "%" + sc.playerPosition.y + 
                                     "%" + sc.playerVelocity.x + "%" + sc.playerVelocity.y +
                                     "%" + sc.playerRotation.z + "%" + sc.playerRotation.w + "|";
            }
            positionRequest = positionRequest.Trim('|');

            // Ask position of clients
            Send(positionRequest, unreliableChannel, clients);
        }
    }

    private void OnConnection(int connId)
    {
        // Add him to a list
        ServerClient sc = new ServerClient();
        sc.connectionID = connId;
        sc.playerName = "TEMP";
        clients.Add(sc);

        // Tell the player his ID on the server for future communication
        // Request his name and send it to all the other players
        string msg = "ASKNAME|" + connId + "|";
        foreach (ServerClient client in clients)
        {
            msg += client.playerName + "%" + client.connectionID + "|";
        }

        msg = msg.Trim('|');

        // Send a message to the clients
        Send(msg, reliableChannel, connId);
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

        // Send his name to all the other players
        Send("CONN|" + playerName + "|" + connID, reliableChannel, clients);
    }
    private void OnMyPosition(int connID, string[] splitData)
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

        // Update the position, velocity, and rotation we have stored
        sc.playerPosition = new Vector2(posX, posY);
        sc.playerVelocity = new Vector2(velX, velY);
        sc.playerRotation = new Quaternion(0, 0, rotZ, rotW);
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
