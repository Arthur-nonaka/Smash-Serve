using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    public Button exitButton;
    public Button teamsButton;
    public Button optionsButton;
    public Button backButton;
    public Button anotherBackButton;

    public GameObject teamPanel;

    public GameObject optionsPanel;
    public GameObject firstPanel;


    void Start()
    {
        exitButton.onClick.AddListener(ExitGame);
        teamsButton.onClick.AddListener(ShowTeamsPanel);
        optionsButton.onClick.AddListener(ShowOptionsPanel);
        backButton.onClick.AddListener(TurnBack);
        anotherBackButton.onClick.AddListener(TurnBack);
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

    public void TurnBack()
    {
        firstPanel.SetActive(true);
        teamPanel.SetActive(false);
        optionsPanel.SetActive(false);

    }
}
