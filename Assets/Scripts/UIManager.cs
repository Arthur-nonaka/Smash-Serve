using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

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

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        ShowNetworkConfigPanel();
    }

    void StartHost()
    {
        SetPlayerName();
        networkManager.StartHost();
        ShowGameplayPanel();
    }

    void StartClient()
    {
        SetPlayerName();
        networkManager.networkAddress = ipInput.text;
        networkManager.StartClient();
        ShowGameplayPanel();
    }

    void SetPlayerName()
    {
        string playerName = playerNameInput.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
            Debug.Log("Player Name Set: " + playerName);
        }
        else
        {
            Debug.LogWarning("Player Name is empty!");
        }
    }

    public void ShowGameplayPanel()
    {
        gameplayPanel.SetActive(true);
        networkConfigPanel.SetActive(false);
    }

    public void ShowNetworkConfigPanel()
    {
        gameplayPanel.SetActive(false);
        networkConfigPanel.SetActive(true);
    }
}
