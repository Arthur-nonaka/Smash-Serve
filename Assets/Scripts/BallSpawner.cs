using UnityEngine;
using Mirror;

public class BallSpawner : NetworkBehaviour
{
    public GameObject ballPrefab;
    public float spawnHeight = 20f;

    void Update()
    {
        if (isLocalPlayer && Input.GetKeyDown(KeyCode.B))
        {
            CmdSpawnBall();
        }

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.V))
        {
            CmdSpawnBallVelocity();
        }
    }

    [Command]
    void CmdSpawnBall()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;
        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        NetworkServer.Spawn(ball);
    }


    [Command]
    void CmdSpawnBallVelocity()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 5f + transform.forward * 7f;

        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        if (ballRb != null)
        {
            ballRb.linearVelocity = transform.forward * -30f;
        }
        else
        {
            Debug.LogError("Rigidbody not found on the ball prefab.");
        }

        NetworkServer.Spawn(ball);
    }
}
