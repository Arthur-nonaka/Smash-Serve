// using System.Collections.Generic;
// using UnityEngine;
// using Mirror;

// public class GameManager : NetworkBehaviour
// {
//     public enum Team
//     {
//         None,
//         Team1,
//         Team2
//     }
//     public static GameManager Instance;

//     public VolleyballBall ball;

//     [Header("Game Rules")]
//     public int maxPlayers = 6;
//     public int maxTouches = 3;

//     [SyncVar]
//     public Team servingTeam = Team.Team1;

//     [SyncVar]
//     public uint lastTouchPlayerNetId = 0;

//     public override void OnStartServer()
//     {
//         Instance = this;
//     }

//     [Server]
//     public void UpdateBallLastTouched(string playerName)
//     {
//         if (ball != null)
//         {
//             ball.SetLastTouchedPlayer(playerName);
//         }
//     }
// }
