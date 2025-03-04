using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

public class UITEam : MonoBehaviour
{
    public Button team1Button;
    public Button team2Button;

    private PlayerController localPlayer;

    void Start()
    {
        if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            localPlayer = NetworkClient.connection.identity.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogError("Local player not found. Ensure the player prefab has PlayerController.");
        }
        team1Button.onClick.AddListener(() => ChooseTeam(Team.Team1));
        team2Button.onClick.AddListener(() => ChooseTeam(Team.Team2));
    }

    void Update()
    {

    }

    void ChooseTeam(Team team)
    {
        localPlayer.CmdSetTeam(team);
    }
}
