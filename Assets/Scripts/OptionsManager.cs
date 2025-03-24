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
    public Button resetButton;
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
            resetButton.onClick.AddListener(ResetBalls);
        }
        else
        {
            matchButton.gameObject.SetActive(false);
            resetButton.gameObject.SetActive(false);
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

    public void ResetBalls()
    {
        if (isServer)
        {
            ResetAllBalls();
        }
        else
        {
            CmdRequestResetBalls();
        }
    }

    [Server]
    private void ResetAllBalls()
    {
        Debug.Log("Server resetting all balls");

        VolleyballBall[] balls = FindObjectsOfType<VolleyballBall>();
        foreach (VolleyballBall ball in balls)
        {
            if (ball != null)
            {
                Debug.Log($"Resetting ball: {ball.name}");
                ball.ServerResetBall();
            }
        }
    }

    [Command]
    private void CmdRequestResetBalls()
    {
        Debug.Log("Server received reset request from client");
        ResetAllBalls();
    }
}
