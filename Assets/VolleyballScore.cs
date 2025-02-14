using UnityEngine;

public class VolleyballScore : MonoBehaviour
{
    private int teamAScore = 0;
    private int teamBScore = 0;

    public void AddPointToTeamA()
    {
        teamAScore++;
        Debug.Log("Team A Score: " + teamAScore);
    }

    public void AddPointToTeamB()
    {
        teamBScore++;
        Debug.Log("Team B Score: " + teamBScore);
    }

    public int GetTeamAScore()
    {
        return teamAScore;
    }

    public int GetTeamBScore()
    {
        return teamBScore;
    }
}