using UnityEngine;
using Mirror;

public class TrainerBallSpawner : NetworkBehaviour
{
    public GameObject ballPrefab;

    [Server]
    public void CmdSpawnBallTutorialBump(Vector3 spawnPosition, Vector3 power)
    {
        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        if (ballRb != null)
        {
            float randomX = Random.Range(-0.6f, 0.6f);
            float randomY = Random.Range(-0.6f, 0.6f);

            ballRb.linearVelocity = new Vector3(power.x + randomX, power.y + randomY, power.z + randomX);
        }
        else
        {
            Debug.LogError("Rigidbody not found on the ball prefab.");
        }

        Debug.Log("Ball spawned at position: " + spawnPosition);
        NetworkServer.Spawn(ball);
    }
}
