using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;

public class BumpHitbox : NetworkBehaviour
{
    public float chargeSpeed = 3.0f;
    public float maxChargeTime = 0.001f;

    private Slider powerSlider;

    public float mancheteForce = 3.0f;
    public Transform playerCamera;

    private float hitChargeTime = 0f;
    private bool isCharging = false;

    private GameObject ball;

    public AnimationController animationController;

    private Collider hitboxCollider;

    private PlayerController playerMovement;

    private NetworkIdentity parentIdentity;

    public PlayerNameTag playerNameTag;

    void Start()
    {
        parentIdentity = GetComponentInParent<NetworkIdentity>();
        if (parentIdentity == null)
        {
            Debug.LogError("No NetworkIdentity found on parent object.");
        }

        powerSlider = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Slider>();
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false;
        playerMovement = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (!isOwned) return;

        if (Input.GetMouseButtonDown(1) && playerMovement.GetIsGrounded())
        {
            Debug.Log("Mouse right button down");
            isCharging = true;
            hitChargeTime = 0f;
            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
            hitboxCollider.enabled = true;
        }
        if (Input.GetMouseButton(0) && isCharging && playerMovement.GetIsGrounded())
        {
            hitChargeTime += Time.deltaTime * chargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }
        if (Input.GetMouseButtonUp(1) && playerMovement.GetIsGrounded())
        {
            Debug.Log("Mouse right button up");
            isCharging = false;
            hitboxCollider.enabled = false;
            powerSlider.value = 0f;
            hitChargeTime = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball") && isCharging)
        {
            ball = other.gameObject;
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 cameraForward = playerCamera.forward;
                Vector3 horizontalDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                float cameraPitchDegrees = playerCamera.eulerAngles.x;
                float cameraPitchRadians = cameraPitchDegrees * Mathf.Deg2Rad;
                float horizontalReductionFactor = Mathf.Clamp01(1 - 0.4f * Mathf.Pow(Mathf.Sin(cameraPitchRadians), 2));
                horizontalDirection *= horizontalReductionFactor;
                float verticalForce = 5f;
                Vector3 bumpDirection = horizontalDirection + Vector3.up * verticalForce;
                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Bump", true, 0.5f));
                float hitPower = mancheteForce * (hitChargeTime + 2f);
                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.right * 0.08f + playerCamera.forward * randomDirection;

                NetworkIdentity ballIdentity = ball.GetComponent<NetworkIdentity>();
                if (ballIdentity != null && parentIdentity != null && parentIdentity.isOwned)
                {
                    CmdBumpBall(ballIdentity.netId, bumpDirection, hitPower, spin);
                    playerMovement.CmdNotifyBallTouched(false);

                }
            }
            hitChargeTime = 0f;
            isCharging = false;
            hitboxCollider.enabled = false;
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
    void CmdBumpBall(uint ballNetId, Vector3 bumpDirection, float hitPower, Vector3 spin)
    {
        if (NetworkServer.spawned.TryGetValue(ballNetId, out NetworkIdentity identity))
        {
            VolleyballBall ball = identity.GetComponent<VolleyballBall>();
            if (ball != null)
            {
                ball.ApplyBump(bumpDirection, hitPower, spin);

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
