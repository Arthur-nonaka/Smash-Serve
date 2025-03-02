// using UnityEngine;
// using Mirror;

// public class RoomManager : NetworkManager
// {
//     // Assign your playerPrefab (set in the NetworkManager component)
//     // and a spawn point in the inspector.
//     public Transform spawnPoints;

//     public override void OnStartServer()
//     {
//         base.OnStartServer();
//         Debug.Log("Server started!");
//     }

//     public override void OnClientConnect(NetworkConnection conn)
//     {
//         base.OnClientConnect(conn);
//         Debug.Log("Client connected to server!");
//     }

//     // This is called on the server when a new client connects.
//     public override void OnServerAddPlayer(NetworkConnection conn)
//     {
//         Debug.Log("Adding player for connection: " + conn.connectionId);
//         // Use the provided spawn point.
//         Transform startPos = spawnPoints;
//         // Instantiate the playerPrefab at the spawn point.
//         GameObject player = Instantiate(playerPrefab, startPos.position, startPos.rotation);
//         // Add the player for this connection.
//         NetworkServer.AddPlayerForConnection(conn, player);
//     }
// }
