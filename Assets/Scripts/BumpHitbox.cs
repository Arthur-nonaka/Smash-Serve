using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

public class BumpHitbox : MonoBehaviourPunCallbacks
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
    private Collider ownershipHitboxCollider;

    void Start()
    {

        powerSlider = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Slider>();
        ownershipHitboxCollider = transform.Find("Hitbox-Bump-Ownership").GetComponent<Collider>();
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false;
        ownershipHitboxCollider.enabled = false;
    }

    void Update()
    {

        if (!photonView.IsMine) return;

        if (Input.GetMouseButtonDown(1) && GetIsGrounded())
        {
            Debug.Log("mouse direito downn");
            isCharging = true;
            hitChargeTime = 0f;

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
            hitboxCollider.enabled = true;
            ownershipHitboxCollider.enabled = true;
        }
        if (Input.GetMouseButton(0) && isCharging && GetIsGrounded())
        {
            hitChargeTime += Time.deltaTime * chargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }
        if (Input.GetMouseButtonUp(1) && GetIsGrounded())
        {
            Debug.Log("mouse direito up");
            isCharging = false;
            hitboxCollider.enabled = false;
            ownershipHitboxCollider.enabled = false;
            powerSlider.value = 0f;
            hitChargeTime = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball") && isCharging)
        {
            ball = other.gameObject;
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            RequestOwnership(ball);
            if (ballRb != null)
            {
                Vector3 cameraForward = playerCamera.transform.forward;

                Vector3 horizontalDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;

                float cameraPitchDegrees = playerCamera.transform.eulerAngles.x;
                float cameraPitchRadians = cameraPitchDegrees * Mathf.Deg2Rad;


                float horizontalReductionFactor = Mathf.Clamp01(1 - 0.4f * Mathf.Pow(Mathf.Sin(cameraPitchRadians), 2));

                horizontalDirection *= horizontalReductionFactor;

                float verticalForce = 5f;

                Vector3 bumpDirection = horizontalDirection + Vector3.up * verticalForce;

                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Bump", true, 0.5f));

                float hitPower = mancheteForce * (hitChargeTime + 2f);

                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.transform.right * 0.08f + playerCamera.transform.forward * randomDirection;

                StartCoroutine(WaitForOwnershipAndBump(ballRb, bumpDirection, hitPower, spin));

                hitChargeTime = 0f;
                isCharging = false;
                hitboxCollider.enabled = false;
                ownershipHitboxCollider.enabled = false;
            }
        }
    }

    IEnumerator WaitForOwnershipAndBump(Rigidbody ballRb, Vector3 bumpDirection, float hitPower, Vector3 spin)
    {

        while (!ball.GetComponent<PhotonView>().IsMine)
        {
            yield return null;
        }

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.AddForce(bumpDirection * hitPower, ForceMode.Impulse);
        ballRb.AddTorque(spin, ForceMode.Impulse);

        // photonView.RPC("SyncBallState", RpcTarget.Others, ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);
    }


    void RequestOwnership(GameObject obj)
    {
        PhotonView photonView = obj.GetComponent<PhotonView>();
        if (photonView != null && !photonView.IsMine)
        {
            Debug.Log($"Requesting ownership of {obj.name}");
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
    }

    [PunRPC]
    void SyncBallState(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
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

    private bool GetIsGrounded()
    {
        return Physics.Raycast((transform.position + Vector3.up * 1f), Vector3.down, out RaycastHit hit, 0.29f);
    }
}