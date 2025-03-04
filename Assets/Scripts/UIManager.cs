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
    public GameObject teamPanel;
    public GameObject configPanel;
    public GameObject optionsPanel;

    public Text lastPlayerNameText;

    private OptionsManager optionsManager;

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        gameplayPanel.SetActive(false);
        networkConfigPanel.SetActive(false);
        teamPanel.SetActive(false);
        optionsPanel.SetActive(false);

        ShowNetworkConfigPanel();

    }

    void Update()
    {
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
        teamPanel.SetActive(false);
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
    }

    public void HideTeamsPanel()
    {
        optionsManager = GameObject.FindGameObjectWithTag("OptionsManager").GetComponent<OptionsManager>();
        optionsManager.TurnBack();
    }

    public void HideOptionsPanel()
    {
        optionsPanel.SetActive(false);
        teamPanel.SetActive(false);
        configPanel.SetActive(false);
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

}
