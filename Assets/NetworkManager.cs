using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;
    public Transform[] spawnPoints;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)  
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ConnectToServer();
    }

    void SpawnPlayer()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn points not assigned!");
            return;
        }

        Transform spawnPoint = spawnPoints[PhotonNetwork.CurrentRoom.PlayerCount - 1];
        PhotonNetwork.Instantiate("PlayerPrefab", spawnPoint.position, spawnPoint.rotation);
    }

    public void ConnectToServer()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby!");
        CreateOrJoinRoom();
    }

    public void CreateOrJoinRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No rooms available, creating a new one.");
        PhotonNetwork.CreateRoom("Sala_" + Random.Range(1000, 9999));
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined a room!");
        SpawnPlayer();
        LoadGameScene();
    }

    void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene"); // Altere para o nome da sua cena de jogo
        }
    }
}
