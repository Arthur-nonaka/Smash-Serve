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
    public PlayerController playerController;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        parentIdentity = GetComponentInParent<NetworkIdentity>();
        if (parentIdentity == null)
        {
            Debug.LogError("No NetworkIdentity found on parent object.");
        }
        hitboxCollider = GetComponent<Collider>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found in the parent hierarchy.");
        }
        else
        {
            Debug.Log("Find playerController");
        }
    }

    public override void OnStartServer()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found on server.");
        }
    }

    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return;

        if (other.CompareTag("Ball"))
        {
            ball = other.gameObject;
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                if (!playerController.IsSameSide())
                {
                    return;
                }

                float hitPower = diveHitForce;
                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.transform.right * 0.08f + playerCamera.transform.forward * randomDirection;

                NetworkIdentity ballIdentity = ball.GetComponent<NetworkIdentity>();
                if (ballIdentity != null && parentIdentity != null && parentIdentity.isOwned)
                {
                    if (playerController.CanTouch())
                    {
                        CmdApplyDiveForce(ballIdentity.netId, hitPower, spin);
                        playerController.CmdNotifyBallTouched(false);
                    }
                }
            }
        }
    }

    [Command]
    void CmdApplyDiveForce(uint ballNetId, float hitPower, Vector3 spin)
    {
        if (NetworkServer.spawned.TryGetValue(ballNetId, out NetworkIdentity identity))
        {
            VolleyballBall ball = identity.GetComponent<VolleyballBall>();

            if (ball != null)
            {
                ball.ApplyBump(Vector3.up, hitPower, spin);
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
