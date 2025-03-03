using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 

    public VolleyballBall ball;

    public override void OnStartServer()
    {
        Instance = this;
    }

    [Server]
    public void UpdateBallLastTouched(string playerName)
    {
        if (ball != null)
        {
            ball.SetLastTouchedPlayer(playerName);
        }
    }
}
