using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;

public class DiveHitbox : NetworkBehaviour
{
    public float diveHitForce = 3.0f;
    public Transform playerCamera;
    private GameObject ball;

    private Collider hitboxCollider;

    private NetworkIdentity parentIdentity;

    void Start()
    {
        parentIdentity = GetComponentInParent<NetworkIdentity>();
        if (parentIdentity == null)
        {
            Debug.LogError("No NetworkIdentity found on parent object.");
        }
        hitboxCollider = GetComponent<Collider>();
    }

    void Update()
    {
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isLocalPlayer) return;

        if (other.CompareTag("Ball"))
        {
            ball = other.gameObject;
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                float hitPower = diveHitForce;
                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.transform.right * 0.08f + playerCamera.transform.forward * randomDirection;

                NetworkIdentity ballIdentity = ball.GetComponent<NetworkIdentity>();
                if (ballIdentity != null && parentIdentity != null && parentIdentity.isOwned)
                {
                    CmdApplyDiveForce(ballIdentity.netId, hitPower, spin);
                }
            }
        }
    }

    [ClientRpc]
    void RpcSyncBallState(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
    {
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ball.transform.position = position;
                ballRb.linearVelocity = velocity;
                ballRb.angularVelocity = angularVelocity;
            }
        }
    }

    [Command]
    void CmdApplyDiveForce(uint ballNetId, float hitPower, Vector3 spin)
    {
        if (NetworkServer.spawned.TryGetValue(ballNetId, out NetworkIdentity identity))
        {
            GameObject ball = identity.gameObject;
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();

            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
                ballRb.AddForce(Vector3.up * hitPower, ForceMode.Impulse);
                ballRb.AddTorque(spin, ForceMode.Impulse);

                RpcSyncBallState(ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);
            }
            else
            {
                Debug.LogError("Rigidbody not found on the ball object.");
            }
        }
        else
        {
            Debug.LogError($"Invalid ballNetId: {ballNetId}. The object may not be spawned or registered in the network.");
        }


    }
}
