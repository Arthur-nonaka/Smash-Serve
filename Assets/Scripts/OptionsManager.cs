using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class OptionsManager : NetworkBehaviour
{
    public Button exitButton;
    public Button teamsButton;
    public Button optionsButton;
    public Button matchButton;
    public Button backButton;
    public Button anotherBackButton;
    public Button anotherBackButton2;

    public GameObject teamPanel;

    public GameObject optionsPanel;
    public GameObject matchPanel;
    public GameObject firstPanel;


    void Start()
    {
        exitButton.onClick.AddListener(ExitGame);
        teamsButton.onClick.AddListener(ShowTeamsPanel);
        optionsButton.onClick.AddListener(ShowOptionsPanel);
        backButton.onClick.AddListener(TurnBack);
        anotherBackButton.onClick.AddListener(TurnBack);
        anotherBackButton2.onClick.AddListener(TurnBack);

        if (isServer)
        {
            matchButton.onClick.AddListener(ShowMatchPanel);
        }
        else
        {
            matchButton.gameObject.SetActive(false);
        }

        firstPanel.SetActive(true);
    }

    void Update()
    {
    }

    void ExitGame()
    {
        Application.Quit();
    }

    void ShowTeamsPanel()
    {
        teamPanel.SetActive(true);
        firstPanel.SetActive(false);
    }

    void ShowOptionsPanel()
    {
        optionsPanel.SetActive(true);
        firstPanel.SetActive(false);
    }

    void ShowMatchPanel()
    {
        matchPanel.SetActive(true);
        firstPanel.SetActive(false);
    }

    public void TurnBack()
    {
        firstPanel.SetActive(true);
        teamPanel.SetActive(false);
        optionsPanel.SetActive(false);
        matchPanel.SetActive(false);

    }
}
