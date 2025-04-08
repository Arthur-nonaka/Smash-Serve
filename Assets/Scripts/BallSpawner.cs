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

        // if (isLocalPlayer && Input.GetKeyDown(KeyCode.E))
        // {
        //     CmdSpawnBallVelocity();
        // }
    }

    [Command]
    void CmdSpawnBall()
    {
        if (TeamManager.Instance.matchActive)
        {
            if (TeamManager.Instance.designatedServerNetId != netId && TeamManager.Instance.designatedServerNetId != 0)
            {
                Debug.Log("Only the designated server can spawn the ball.");
                return;
            }
            if (FindObjectOfType<VolleyballBall>() != null)
            {
                Debug.Log("A ball already exists in the scene. Cannot spawn another one.");
                return;
            }

            TeamManager.Instance.currentTouches = 2;
        }


        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;
        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        NetworkServer.Spawn(ball);
    }

    [Command]
    void CmdSpawnBallVelocity()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 5f + transform.forward * 11f;

        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        if (ballRb != null)
        {
            ballRb.linearVelocity = transform.forward * -23f;
        }
        else
        {
            Debug.LogError("Rigidbody not found on the ball prefab.");
        }

        NetworkServer.Spawn(ball);
    }

    [Command]
    public void CmdSpawnBallTutorialBump()
    {
        Vector3 spawnPosition = new Vector3(-11, 6, -40);

        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        if (ballRb != null)
        {
            ballRb.linearVelocity = new Vector3(10, 5, 0);
        }
        else
        {
            Debug.LogError("Rigidbody not found on the ball prefab.");
        }

        NetworkServer.Spawn(ball);
    }
}
