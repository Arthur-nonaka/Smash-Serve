using UnityEngine;
using Mirror;

public class MyNetworkManager : NetworkManager
{
    public Transform spawnPoints;

    public override void Start()
    {
        // Debug.Log("Attempting to join an existing server...");
        // StartClient();
        // Invoke(nameof(CheckClientConnection), 5f); 
    }

    void CheckClientConnection()
    {
        if (!NetworkClient.isConnected)
        {
            Debug.Log("No existing server found. Starting a new host...");
            StartHost();
        }
        else
        {
            Debug.Log("Connected to an existing server.");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started!");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Client started and connected to server!");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("Adding player for connection: " + conn.connectionId);
        Transform startPos = spawnPoints;
        GameObject player = Instantiate(playerPrefab, startPos.position, startPos.rotation);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (NetworkClient.isConnected)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}