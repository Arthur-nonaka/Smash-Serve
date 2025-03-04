using System.Collections.Generic;
using TMPro;
using Mirror;
using UnityEngine.UI;
using UnityEngine;

// public enum Team
// {
//     None,
//     Team1,
//     Team2
// }

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance;

    [Header("Spawn Points")]
    public List<Transform> team1SpawnPoints = new List<Transform>();
    public List<Transform> team2SpawnPoints = new List<Transform>();


    [Header("Score")]
    public TMP_Text point1Text;
    public TMP_Text point2Text;

    public int score1 = 0;
    public int score2 = 0;


    public VolleyballBall ball;

    [Header("Game Rules")]
    public int maxPlayers = 20;

    public int maxPlayersPerTeam = 6;
    public int maxTouches = 3;

    [SyncVar]
    public Team servingTeam = Team.Team1;

    [SyncVar]
    public int currentTouches = 0;

    [SyncVar]
    public uint lastTouchPlayerNetId = 0;

    public List<PlayerController> players = new List<PlayerController>();
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        if (players.Count >= maxPlayers)
        {
            Debug.LogWarning("Maximum players reached.");
            return;
        }
        players.Add(player);
        Debug.Log($"Player {player.netId} registered.");
    }

    [Server]
    public void UnregisterPlayer(PlayerController player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($"Player {player.netId} unregistered.");
        }
    }

    [Server]
    public void UpdateBallLastTouched(PlayerController player)
    {
        if (lastTouchPlayerNetId == player.netId)
        {
            Debug.LogWarning($"Fault! Player {player.netId} touched twice.");
            EndRally(player.team == Team.Team1 ? Team.Team2 : Team.Team1);
            return;
        }

        currentTouches++;
        lastTouchPlayerNetId = player.netId;

        if (currentTouches > maxTouches)
        {
            EndRally(player.team == Team.Team1 ? Team.Team2 : Team.Team1);
        }
        else
        {
            Debug.Log($"Ball touched by player {player.netId}. Current touches: {currentTouches}");
        }
    }

    [Server]
    public void BallFellOnGround(Team team)
    {
        if (team == Team.Team1)
        {
            EndRally(Team.Team2);
        }
        else if (team == Team.Team2)
        {
            EndRally(Team.Team1);
        }
        else if (lastTouchPlayerNetId != 0)
        {
            PlayerController lastTouchPlayer = players.Find(p => p.netId == lastTouchPlayerNetId);
            if (lastTouchPlayer != null)
            {
                EndRally(lastTouchPlayer.team == Team.Team1 ? Team.Team2 : Team.Team1);
            }
            else
            {
                Debug.LogWarning("Last touch player not found.");
            }
        }

    }

    [Server]
    void ResetTouches()
    {
        currentTouches = 0;
        lastTouchPlayerNetId = 0;
    }

    [Server]
    void EndRally(Team winningTeam)
    {
        servingTeam = winningTeam;
        ball.CmdResetBall();
        NotificationManager.Instance.QueueNotification($"{winningTeam}`s point", Color.green);
        if (winningTeam == Team.Team1)
        {
            score1++;
        }
        else
        {
            score2++;
        }
        UpdateScoreUI();
        ResetTouches();
        Debug.Log($"Rally ended. Winning team: {winningTeam}. New serving team: {servingTeam}");
    }

    [ClientRpc]
    void RpcUpdateScoreUI(int team1Score, int team2Score)
    {
        if (point1Text != null)
        {
            point1Text.text = $"{team1Score}";
        }
        if (point2Text != null)
        {
            point2Text.text = $"{team2Score}";
        }
    }

    [Server]
    void UpdateScoreUI()
    {
        RpcUpdateScoreUI(score1, score2);
    }

    [Server]
    public void SetPlayerTeam(PlayerController player, Team team)
    {
        player.team = team;
        if (team == Team.Team1)
        {
            if (players.FindAll(p => p.team == Team.Team1).Count >= maxPlayersPerTeam)
            {
                Debug.LogWarning("Team 1 is full.");
                return;
            }
            player.transform.position = team1SpawnPoints[Random.Range(0, team1SpawnPoints.Count)].position;
        }
        else if (team == Team.Team2)
        {
            if (players.FindAll(p => p.team == Team.Team2).Count >= maxPlayersPerTeam)
            {
                Debug.LogWarning("Team 2 is full.");
                return;
            }
            player.transform.position = team2SpawnPoints[Random.Range(0, team2SpawnPoints.Count)].position;
        }
        Debug.Log($"Player {player.netId} assigned to team {team}");
    }


    [Server]
    public void RegisterBall(VolleyballBall newBall)
    {
        ball = newBall;
        Debug.Log($"Ball registered in TeamManager: {ball.netId}");
    }
}
