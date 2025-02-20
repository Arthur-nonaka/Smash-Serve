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

    // Reference to the ball (set externally)
    public GameObject ball;

    public AnimationController animationController;


    private Collider hitboxCollider;

    void Start()
    {

        powerSlider = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Slider>();
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false;
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("mouse direito downn");
            isCharging = true;
            hitChargeTime = 0f;

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
            hitboxCollider.enabled = true;
        }
        if (Input.GetMouseButton(0) && isCharging)
        {
            hitChargeTime += Time.deltaTime * chargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }
        if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("mouse direito up");
            isCharging = false;
            hitboxCollider.enabled = false;
            powerSlider.value = 0f;
            hitChargeTime = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && isCharging)
        {
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


                float horizontalReductionFactor = Mathf.Clamp01(1 - 0.4f * Mathf.Pow(Mathf.Sin(cameraPitchRadians), 2));

                horizontalDirection *= horizontalReductionFactor;

                float verticalForce = 4f;

                Vector3 bumpDirection = horizontalDirection + Vector3.up * verticalForce;

                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Bump", true, 0.5f));

                float hitPower = mancheteForce * (hitChargeTime + 1f);
                ballRb.linearVelocity = Vector3.zero;

                ballRb.AddForce(bumpDirection.normalized * hitPower, ForceMode.Impulse);
                Debug.Log("Bump performed with force: " + hitChargeTime);


                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.transform.right * 0.08f + playerCamera.transform.forward * randomDirection;
                ballRb.AddTorque(spin, ForceMode.Impulse);

                hitChargeTime = 0f;
                isCharging = false;
                hitboxCollider.enabled = false;
            }
        }
    }
}