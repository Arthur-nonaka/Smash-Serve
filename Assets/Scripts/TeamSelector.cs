using System.Collections.Generic;
using TMPro;
using Mirror;
using UnityEngine.UI;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance;

    [Header("Spawn Points")]
    public List<Transform> team1SpawnPoints = new List<Transform>();
    public List<Transform> team2SpawnPoints = new List<Transform>();
    public Transform ServeTeam1;
    public Transform ServeTeam2;


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
    [SyncVar]
    public uint designatedServerNetId = 0;

    public List<PlayerController> players = new List<PlayerController>();

    [Header("Match Settings")]
    [SyncVar]
    public bool matchActive = false;

    public List<PlayerController> team1Players = new List<PlayerController>();
    public List<PlayerController> team2Players = new List<PlayerController>();

    [Header("Team Colors")]
    public Material team1Color;
    public Material team2Color;


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

        if (player.team == Team.Team1 && team1Players.Contains(player))
        {
            team1Players.Remove(player);
        }
        else if (player.team == Team.Team2 && team2Players.Contains(player))
        {
            team2Players.Remove(player);
        }
    }

    [Server]
    public bool AreTeamsReady()
    {
        return team1Players.Count > 0 && team2Players.Count > 0;
    }

    [Server]
    public void StartMatch()
    {
        if (!AreTeamsReady())
        {
            Debug.LogWarning("Cannot start match: both teams must have at least one player.");
            return;
        }

        score1 = 0;
        score2 = 0;

        matchActive = true;
        VolleyballBall[] balls = FindObjectsOfType<VolleyballBall>();
        foreach (VolleyballBall ball in balls)
        {
            Destroy(ball.gameObject);
        }
        UpdateScoreUI();
        TeleportPlayersToSpawnPoints();

        PlayerController designatedServer = players.Find(p => p.netId == designatedServerNetId);
        if (designatedServer != null)
        {
            RpcQueueNotification($"Server: Player {designatedServer.GetPlayerName()}", Color.gray);
        }

        Debug.Log("Match started!");
    }

    [Server]
    public void TeleportPlayersToSpawnPoints()
    {
        foreach (PlayerController player in team1Players)
        {
            Vector3 spawnPosition = Vector3.zero;
            if (team1SpawnPoints.Count > 0)
            {
                spawnPosition = team1SpawnPoints[Random.Range(0, team1SpawnPoints.Count)].position;
            }
            else
            {
                Debug.LogWarning("No spawn points defined for Team1.");
            }
            player.RpcTeleport(spawnPosition);
        }

        foreach (PlayerController player in team2Players)
        {
            Vector3 spawnPosition = Vector3.zero;
            if (team2SpawnPoints.Count > 0)
            {
                spawnPosition = team2SpawnPoints[Random.Range(0, team2SpawnPoints.Count)].position;
            }
            else
            {
                Debug.LogWarning("No spawn points defined for Team2.");
            }
            player.RpcTeleport(spawnPosition);
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

        PlayerController previousPlayer = players.Find(p => p.netId == lastTouchPlayerNetId);
        if (previousPlayer != null)
        {
            previousPlayer.RpcHideLastTouchedMessage();
        }

        player.RpcShowLastTouchedMessage();

        if (player.team != players.Find(p => p.netId == lastTouchPlayerNetId)?.team)
        {
            currentTouches = 0;
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
    public void UpdateBallLastTouchedBlock(PlayerController player)
    {
        lastTouchPlayerNetId = player.netId;
    }

    [Server]
    public void BallFellOnGround(Team team)
    {
        if (!matchActive)
        {
            return;
        }

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
        else
        {
            EndRally(servingTeam == Team.Team1 ? Team.Team2 : Team.Team1);
        }

    }

    [Server]
    public void BallPassedOffAntenna(VolleyballBall ball)
    {
        if (currentTouches >= 2)
        {
            PlayerController lastTouchPlayer = players.Find(p => p.netId == lastTouchPlayerNetId);
            if (lastTouchPlayer != null)
            {
                EndRally(lastTouchPlayer.team == Team.Team1 ? Team.Team2 : Team.Team1);
            }
            else
            {
                EndRally(servingTeam == Team.Team1 ? Team.Team2 : Team.Team1);
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
        ball.CmdResetBall();
        RpcQueueNotification($"{winningTeam}`s point", Color.green);
        if (winningTeam == Team.Team1)
        {
            score1++;
            if (team1Players.Count > 0)
            {
                if (servingTeam != Team.Team1)
                {
                    designatedServerNetId = team1Players[0].netId;
                    PlayerController server = team1Players[0];
                    team1Players.RemoveAt(0);
                    team1Players.Add(server);
                }
            }
        }
        else if (winningTeam == Team.Team2)
        {
            score2++;
            if (team2Players.Count > 0)
            {
                if (servingTeam != Team.Team2)
                {
                    designatedServerNetId = team2Players[0].netId;
                    PlayerController server = team2Players[0];
                    team2Players.RemoveAt(0);
                    team2Players.Add(server);
                }
            }
        }
        else
        {
            designatedServerNetId = 0;
        }
        servingTeam = winningTeam;
        PlayerController designatedServer = players.Find(p => p.netId == designatedServerNetId);
        if (designatedServer != null)
        {
            RpcQueueNotification($"Server: {designatedServer.GetPlayerName()}", Color.gray);
            if(designatedServer.team == Team.Team1)
            {
                designatedServer.RpcTeleport(ServeTeam1.position);
            }
            else if (designatedServer.team == Team.Team2)
            {
                designatedServer.RpcTeleport(ServeTeam2.position);
            }
        }
        UpdateScoreUI();
        ResetTouches();
        ball = null;
        Debug.Log($"Rally ended. Winning team: {winningTeam}. New serving team: {servingTeam}");

        if (score1 >= 25 || score2 >= 25)
        {
            if (score1 - score2 >= 2 || score1 - score2 <= -2)
            {
                EndMatch(winningTeam);
            }
        }
    }

    [ClientRpc]
    public void RpcQueueNotification(string message, Color color)
    {
        NotificationManager.Instance.QueueNotification(message, color);
    }

    [Server]
    void EndMatch(Team winningTeam)
    {
        matchActive = false;
        ResetTouches();
        ball = null;

        RpcQueueNotification($"Match Over! {winningTeam} Wins!", Color.yellow);

        score1 = 0;
        score2 = 0;
        UpdateScoreUI();

        foreach (PlayerController player in players)
        {
            Vector3 lobbyPosition = Vector3.zero;
            player.RpcTeleport(lobbyPosition);
        }

        Debug.Log($"Match ended. Winning team: {winningTeam}");
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
        if (team1Players.Contains(player))
        {
            team1Players.Remove(player);
            Debug.Log("Removing player from Team 1");

        }
        if (team2Players.Contains(player))
        {
            Debug.Log("Removing player from Team 2");
            team2Players.Remove(player);
        }

        player.team = team;
        Vector3 spawnPosition = Vector3.zero;
        if (team == Team.Team1)
        {
            if (players.FindAll(p => p.team == Team.Team1).Count >= maxPlayersPerTeam)
            {
                Debug.LogWarning("Team 1 is full.");
                return;
            }
            spawnPosition = team1SpawnPoints[Random.Range(0, team1SpawnPoints.Count)].position;
            team1Players.Add(player);
        }
        else if (team == Team.Team2)
        {
            if (players.FindAll(p => p.team == Team.Team2).Count >= maxPlayersPerTeam)
            {
                Debug.LogWarning("Team 2 is full.");
                return;
            }
            spawnPosition = team2SpawnPoints[Random.Range(0, team2SpawnPoints.Count)].position;
            team2Players.Add(player);
        }
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        renderers[1].material = team == Team.Team1 ? team1Color : team2Color;
        player.RpcSetTeamColor(team == Team.Team1 ? 1 : 2);


        player.RpcTeleport(spawnPosition);
        Debug.Log($"Player {player.netId} assigned to team {team}");
    }


    [Server]
    public void RegisterBall(VolleyballBall newBall)
    {
        if (ball == null)
        {
            ball = newBall;
            Debug.Log($"Ball registered in TeamManager: {ball.netId}");
        }
    }
}
