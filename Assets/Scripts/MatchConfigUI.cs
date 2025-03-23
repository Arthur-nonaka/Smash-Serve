using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MatchConfigUI : MonoBehaviour
{
    public GameObject playerEntryPrefab;
    public Button startMatchButton;
    public Transform team1Content;
    public Transform team2Content;

    private List<PlayerController> players;

    void OnEnable()
    {
        PopulatePlayerLists();
    }

    void Start()
    {
        players = TeamManager.Instance.players;
        startMatchButton.onClick.AddListener(StartMatch);
        PopulatePlayerLists();
    }

    void PopulatePlayerLists()
    {
        foreach (Transform child in team1Content)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in team2Content)
        {
            Destroy(child.gameObject);
        }

        foreach (PlayerController player in players)
        {
            GameObject entry = Instantiate(playerEntryPrefab);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = player.GetPlayerName();
            if (player.team == Team.Team1)
            {
                entry.transform.SetParent(team1Content, false);
            }
            else if (player.team == Team.Team2)
            {
                entry.transform.SetParent(team2Content, false);
            }
        }
    }

    void StartMatch()
    {
        TeamManager.Instance.StartMatch();
    }
}
