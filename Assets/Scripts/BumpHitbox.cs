using UnityEngine;
using Photon.Pun;

public class BumpHitbox : MonoBehaviourPunCallbacks
{
    public float minBumpForce = 5f;
    public float maxBumpForce = 20f;
    public float chargeSpeed = 3.0f;

    public float mancheteForce = 3.0f;
    public Transform playerCamera;

    private float hitChargeTime = 0f;
    private bool isCharging = false;

    // Reference to the ball (set externally)
    public GameObject ball;

    public AnimationController animationController;


    private Collider hitboxCollider;

    void Start()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false;
    }

    void Update()
    {
        // Start charging when right mouse is held
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("mouse direito downn");
            isCharging = true;
            hitChargeTime = minBumpForce;
            hitboxCollider.enabled = true;
        }
        if (Input.GetMouseButton(0) && isCharging)  // Botão 0, geralmente o botão esquerdo do mouse
        {
            hitChargeTime += Time.deltaTime * chargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, minBumpForce, maxBumpForce);
        }
        if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("mouse direito up");
            isCharging = false;
            hitboxCollider.enabled = false;
            // If no ball entered the hitbox during the charge, you can reset the charge here.
            hitChargeTime = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && isCharging)
        {
            // Apply bump force immediately when the ball enters the hitbox
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                PhotonView ballPhotonView = other.GetComponent<PhotonView>();
                if (ballPhotonView != null && !ballPhotonView.IsMine)
                {
                    ballPhotonView.RequestOwnership();
                }
                Vector3 cameraForward = playerCamera.transform.forward;

                Vector3 horizontalDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;

                float cameraPitchDegrees = playerCamera.transform.eulerAngles.x;
                float cameraPitchRadians = cameraPitchDegrees * Mathf.Deg2Rad;


                float horizontalReductionFactor = Mathf.Clamp01(1 - Mathf.Abs(Mathf.Sin(cameraPitchRadians)));

                horizontalDirection *= horizontalReductionFactor;

                float verticalForce = 10f;

                Vector3 bumpDirection = horizontalDirection + Vector3.up * verticalForce;

                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Bump", true, 0.5f));

                float hitPower = mancheteForce * (hitChargeTime + 0.2f);
                ballRb.AddForce(bumpDirection.normalized * hitPower, ForceMode.Impulse);
                Debug.Log("Bump performed with force: " + hitChargeTime);

                hitChargeTime = 0f;
                isCharging = false;
                hitboxCollider.enabled = false;
            }
        }
    }
}