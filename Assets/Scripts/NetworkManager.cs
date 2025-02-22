using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    [Space]

    public Transform spawnPoints;
    void Start()
    {
        PhotonNetwork.SendRate = 40;
        PhotonNetwork.SerializationRate = 20;

        Debug.Log("Creating or joining room...");

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Connected to Photon Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("Joined Lobby!");
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions { MaxPlayers = 14 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Joined Room!");

        GameObject _player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoints.position, spawnPoints.rotation);

    }
    void Update()
    {

    }
}
