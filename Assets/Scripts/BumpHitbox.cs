using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;

public class BumpHitbox : NetworkBehaviour
{
    public float chargeSpeed = 3.0f;
    public float maxChargeTime = 0.001f;

    private Image powerCircle;

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

        powerCircle = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Image>();
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false;
        playerMovement = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (!isOwned) return;

        if (Input.GetMouseButtonDown(1) && playerMovement.GetIsGrounded() && !playerMovement.isServing)
        {
            Debug.Log("Mouse right button down");
            isCharging = true;
            hitChargeTime = 0f;
            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerCircle.fillAmount = powerPercent / 100f;
            hitboxCollider.enabled = true;
        }
        if (Input.GetMouseButton(0) && isCharging && playerMovement.GetIsGrounded())
        {
            hitChargeTime += Time.deltaTime * chargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerCircle.fillAmount = powerPercent / 100f;
        }
        if (Input.GetMouseButtonUp(1) && playerMovement.GetIsGrounded() && !playerMovement.isServing)
        {
            Debug.Log("Mouse right button up");
            isCharging = false;
            hitboxCollider.enabled = false;
            powerCircle.fillAmount = 0f;
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
                if (!playerMovement.IsSameSide(ball) || !playerMovement.GetIsGrounded())
                {
                    return;
                }


                Vector3 cameraForward = playerCamera.forward;
                Vector3 horizontalDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                horizontalDirection *= 1.7f;
                float cameraPitchDegrees = playerCamera.eulerAngles.x;
                float cameraPitchRadians = cameraPitchDegrees * Mathf.Deg2Rad;
                float horizontalReductionFactor = Mathf.Clamp01(1 - 0.4f * Mathf.Pow(Mathf.Sin(cameraPitchRadians), 2));
                horizontalDirection *= horizontalReductionFactor;
                float verticalForce = 4f;

                if (Input.GetKey(KeybindManager.Instance.GetKey("Backwards")) || Input.GetKey(KeyCode.RightControl))
                {
                    horizontalDirection = -horizontalDirection;
                }

                Vector3 bumpDirection = horizontalDirection + Vector3.up * verticalForce;


                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Bump", true, 0.5f));
                float hitPower = mancheteForce * (hitChargeTime + 2f);
                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.right * 0.08f + playerCamera.forward * randomDirection;

                Vector3 hitboxCenter = hitboxCollider.bounds.center;
                Vector3 relativeHit = ball.transform.position - hitboxCenter;
                Vector3 localHit = transform.InverseTransformDirection(relativeHit);
                if (localHit.x < -0.6f)
                {
                    Debug.Log("Left side");
                    float lateralForce = Random.Range(-0.1f, -1f);
                    float verticalForceRandom = Random.Range(-1f, 1f);
                    bumpDirection += transform.right * lateralForce + transform.forward * verticalForceRandom;
                }
                else if (localHit.x > 0.6f)
                {
                    float lateralForce = Random.Range(0.3f, 1f);
                    float verticalForceRandom = Random.Range(-1f, 1f);
                    bumpDirection += transform.right * lateralForce + transform.forward * verticalForceRandom;
                }

                NetworkIdentity ballIdentity = ball.GetComponent<NetworkIdentity>();
                if (ballIdentity != null && parentIdentity != null && parentIdentity.isOwned)
                {
                    if (playerMovement.CanTouch())
                    {
                        CmdBumpBall(ballIdentity.netId, bumpDirection, hitPower, spin);
                        playerMovement.CmdNotifyBallTouched(false);
                    }

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

                StartCoroutine(DisplayTrajectoryAfterPhysicsUpdate(ball));
                // ball.GetComponentInChildren<TrajectoryDrawer>().TriggerTrajectoryDisplay();

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

    IEnumerator DisplayTrajectoryAfterPhysicsUpdate(VolleyballBall ball)
    {
        yield return new WaitForFixedUpdate();

        TrajectoryDrawer trajectoryDrawer = ball.GetComponentInChildren<TrajectoryDrawer>();
        if (trajectoryDrawer != null)
        {
            trajectoryDrawer.TriggerTrajectoryDisplay();
        }
    }
}
