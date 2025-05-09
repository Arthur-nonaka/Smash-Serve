using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Name and Connection UI")]
    public TMP_InputField playerNameInput;
    public TMP_InputField ipInput;
    public TMP_InputField portInput;
    public Button hostButton;
    public Button clientButton;
    public NetworkManager networkManager;
    public GameObject gameplayPanel;
    public GameObject networkConfigPanel;
    public GameObject teamPanel;
    public GameObject configPanel;
    public GameObject firstPanel;
    public GameObject matchPanel;
    public GameObject optionsPanel;

    public Text lastPlayerNameText;

    [Header("Connection Timeout Settings")]
    public float connectionTimeout = 7f;
    public GameObject loadingPanel;

    private OptionsManager optionsManager;

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        gameplayPanel.SetActive(false);
        networkConfigPanel.SetActive(false);
        teamPanel.SetActive(false);
        optionsPanel.SetActive(false);
        matchPanel.SetActive(false);

        ShowNetworkConfigPanel();

    }

    void Update()
    {
    }

    void StartHost()
    {
        SetPlayerName(playerNameInput.text);
        if (SteamLobby.Instance != null)
        {
            Debug.Log("SteamLobby instance found. Hosting lobby...");
            SteamLobby.Instance.HostLobby();
            SetPlayerSteamName();
            ShowGameplayPanel();
        }
        else
        {
            Debug.LogError("SteamLobby instance is null! Ensure SteamLobby is initialized.");
            networkManager.StartHost();
            ShowGameplayPanel();
        }

    }

    void StartClient()
    {
        SetPlayerName(playerNameInput.text);
        networkManager.networkAddress = ipInput.text;
        networkManager.StartClient();
        loadingPanel.SetActive(true);

        StartCoroutine(CheckClientConnection(connectionTimeout));

    }

    IEnumerator CheckClientConnection(float timeout)
    {
        float startTime = Time.time;

        while (!NetworkClient.isConnected && Time.time - startTime < timeout)
        {
            yield return null;
        }

        if (!NetworkClient.isConnected)
        {
            Debug.Log("Client disconnected");
            loadingPanel.SetActive(false);

        }
        else
        {
            Debug.Log("Client connected successfully to server!");
            loadingPanel.SetActive(false);
            ShowGameplayPanel();
        }
    }

    bool SetPlayerSteamName()
    {
        string playerName = playerNameInput.text;

        if (string.IsNullOrEmpty(playerName) && SteamLobby.Instance != null)
        {
            playerName = SteamLobby.Instance.GetSteamUserName();
            playerNameInput.text = playerName;
        }

        if (!string.IsNullOrEmpty(playerName))
        {
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
            Debug.Log("Player Name Set: " + playerName);
            return true;
        }
        else
        {
            Debug.LogWarning("Player Name is empty!");
            return false;
        }
    }

    public void SetPlayerName(string name)
    {
        if (playerNameInput != null)
        {
            playerNameInput.text = name;
        }

        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();

        Debug.Log("Player name updated in UI: " + name);
    }

    public void ShowGameplayPanel()
    {
        gameplayPanel.SetActive(true);
        networkConfigPanel.SetActive(false);
        teamPanel.SetActive(false);
        optionsPanel.SetActive(false);
        Debug.Log("Gameplay panel activated.");
    }

    public void ShowNetworkConfigPanel()
    {
        gameplayPanel.SetActive(false);
        networkConfigPanel.SetActive(true);
        teamPanel.SetActive(false);
    }

    public void ShowOptionsPanel()
    {
        optionsPanel.SetActive(true);
        networkConfigPanel.SetActive(false);
        teamPanel.SetActive(false);
        matchPanel.SetActive(false);
    }

    public void HideTeamsPanel()
    {
        optionsManager = GameObject.FindGameObjectWithTag("OptionsManager").GetComponent<OptionsManager>();
        optionsManager.TurnBack();
    }

    public void HideOptionsPanel()
    {
        optionsPanel.SetActive(false);
        matchPanel.SetActive(false);
        teamPanel.SetActive(false);
        configPanel.SetActive(false);
        firstPanel.SetActive(true);
    }

    public void ShowTeamPanel()
    {
        networkConfigPanel.SetActive(false);
        teamPanel.SetActive(true);
    }

    public void SetLastPlayerName(string name, Color color)
    {
        if (lastPlayerNameText != null)
        {
            lastPlayerNameText.text = name;
            lastPlayerNameText.color = color;
        }
    }

    public bool IsOptionsMenuActive()
    {
        return optionsPanel.activeSelf;
    }

}
