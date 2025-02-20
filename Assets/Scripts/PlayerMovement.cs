using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public CharacterController controller;
    public Camera playerCamera;

    public Animator animator;
    public AnimationController animationController;

    public GameObject setHitbox;
    public GameObject spikeHitbox;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpForce = 1f;
    public float jumpChargeSpeed = 4.5f;
    public float maxHitPower = 20f;
    public float setForce = 10f;
    public Transform handPositionR;
    public Transform handPositionL;
    public float powerChargeSpeed = 3.0f;
    public float spikeForce = 10f;
    public float maxChargeTime = 0.001f;
    public float mouseSensitivity = 2f;
    public float delayTime = 0.2f;
    public float durationOnAir = 0.7f;
    public GameObject ballPrefab;
    private Slider powerSlider;
    private Slider jumpSlider;

    private Vector3 velocity;
    private float chargeTime = 0f;
    private float chargeTimeReverse = 0f;
    private float hitChargeTime = 0f;
    private float verticalRotation = 0f;
    private GameObject ball;
    private bool canSet = false;
    private bool canSpike = false;
    private bool stopJump = false;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        int playerLayer = LayerMask.NameToLayer("Player");
        int ballLayer = LayerMask.NameToLayer("Ball");

        if (playerLayer == -1 || ballLayer == -1)
        {
            Debug.LogError("Player or Ball layer not found. Please ensure the layers are correctly set up.");
        }
        else
        {
            Physics.IgnoreLayerCollision(playerLayer, ballLayer);
        }

        if (!photonView.IsMine)
        {
            // Disable the PlayerController for remote players instead of destroying it
            enabled = false;
            playerCamera.gameObject.SetActive(false);
        }

        powerSlider = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Slider>();
        jumpSlider = GameObject.FindGameObjectWithTag("jumpSlider").GetComponent<Slider>();

        powerSlider.minValue = 0;
        powerSlider.maxValue = 100;
        powerSlider.value = 0;

        jumpSlider.minValue = 0;
        jumpSlider.maxValue = 100;
        jumpSlider.value = 0;


    }

    void Update()
    {
        if (!photonView.IsMine) return;

        HandleMovement();
        HandleJump();
        HandleCamera();
        HandleBallInteraction();
        HandleSpike();


        // Levantamento para frente
        if (Input.GetKey(KeyCode.F)) // Charge the set
        {
            hitChargeTime += Time.deltaTime * powerChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
            animator.SetBool("IsSetting", true);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetKeyUp(KeyCode.F)) // Set in the direction you're looking
        {
            animator.SetBool("IsSetting", false);
            float power = hitChargeTime;
            StartCoroutine(DelayedHitboxCheck(setHitbox, () => PerformSet(power, 1), delayTime));
            hitChargeTime = 0f;

            powerSlider.value = 0;
        }

        if (Input.GetKey(KeyCode.R)) // Charge the set
        {
            hitChargeTime += Time.deltaTime * powerChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
            animator.SetBool("IsSetting", true);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetKeyUp(KeyCode.R)) // Set in the direction you're looking
        {
            animator.SetBool("IsSetting", false);
            float power = hitChargeTime;
            StartCoroutine(DelayedHitboxCheck(setHitbox, () => PerformSet(power, -1), delayTime));
            hitChargeTime = 0f;

            powerSlider.value = 0;
        }
    }

    public IEnumerator SetAnimatorBoolWithDelay(string parameter, bool value, float delay)
    {
        animator.SetBool(parameter, value);
        yield return new WaitForSeconds(delay);
        animator.SetBool(parameter, !value);
    }

    void HandleSpike()
    {
        if (Input.GetMouseButton(0) && !GetIsGrounded())
        {
            hitChargeTime += Time.deltaTime * powerChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetMouseButtonUp(0) && !GetIsGrounded())
        {
            float power = hitChargeTime;
            StartCoroutine(DelayedHitboxCheck(spikeHitbox, () => PerformSpike(power), delayTime));
            hitChargeTime = 0f;

            powerSlider.value = 0;
        }
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        animator.SetFloat("Horizontal", moveX);
        animator.SetFloat("Vertical", moveZ);
        animator.SetFloat("Speed", move.sqrMagnitude);

        if (moveZ > 0 && Input.GetKey(KeyCode.LeftShift))
        {
            animator.SetFloat("Vertical", moveZ + 1);
        }

        float speed = walkSpeed;

        if (GetIsGrounded())
        {
            speed = (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;

            if (Input.GetMouseButton(1))
            {
                speed *= 0.5f;
                animator.SetBool("IsBumping", true);
            }
            else
            {
                animator.SetBool("IsBumping", false);
                if (Input.GetKey(KeyCode.Space))
                {
                    speed *= 0.5f;
                }
            }


        }
        else
        {
            speed *= 0.5f;
        }

        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetKey(KeyCode.Space) && GetIsGrounded() && !stopJump)
        {
            animator.SetBool("IsApproach", true);
            if (chargeTimeReverse > 0)
            {
                chargeTime -= chargeTimeReverse;
                chargeTimeReverse = Time.deltaTime * jumpChargeSpeed;
                if (chargeTime <= 0)
                {
                    animator.SetBool("IsApproach", false);
                    stopJump = true;
                    chargeTime = 0f;
                    chargeTimeReverse = 0f;
                    jumpSlider.value = 0;
                }
            }
            else
            {
                chargeTime += Time.deltaTime * jumpChargeSpeed;
                if (chargeTime >= maxChargeTime)
                {
                    chargeTimeReverse = Time.deltaTime * jumpChargeSpeed;
                }
            }

            float jumpPercent = (chargeTime / Mathf.Abs(maxChargeTime)) * 100f;
            jumpSlider.value = jumpPercent;
        }

        if (Input.GetKeyUp(KeyCode.Space) && GetIsGrounded())
        {
            animator.SetBool("IsApproach", false);
            if (!stopJump)
            {
                float jumpPower = jumpForce * (chargeTime / maxChargeTime + 0.5f);
                velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
                chargeTime = 0f;
                chargeTimeReverse = 0f;
                jumpSlider.value = 0;
            }
            stopJump = false;
        }

        if (velocity.y > -1.5f && velocity.y < 0)
        {
            velocity.y *= durationOnAir;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private bool GetIsGrounded()
    {
        return Physics.Raycast((transform.position + Vector3.up * 1f), Vector3.down, out RaycastHit hit, 0.28f);
    }

    void PerformSet(float power, int directionMultiplier)
    {
        if (!canSet) return;

        if (ball != null)
        {
            RequestOwnership(ball);
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                StartCoroutine(HoldAndSetBall(ballRb, power, directionMultiplier));
            }
        }
    }

    IEnumerator HoldAndSetBall(Rigidbody ballRb, float power, int directionMultiplier)
    {

        Vector3 startPosition = ball.transform.position;
        Vector3 handMidPoint = (handPositionL.position + handPositionR.position) / 2;
        Vector3 midpoint = new Vector3(handMidPoint.x, transform.position.y * -7.1f, handMidPoint.z);
        midpoint += Vector3.back * 0.15f;
        float duration = 0.16f;
        float elapsedTime = 0f;

        ballRb.isKinematic = true;

        while (elapsedTime < duration)
        {
            ball.transform.position = Vector3.Lerp(startPosition, midpoint, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (directionMultiplier >= 1)
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("Front_Set", true, 0.5f));
        }
        else
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("Back_Set", true, 0.5f));
        }

        ball.transform.position = midpoint;

        yield return new WaitForSeconds(0.005f);

        Vector3 setDirection = playerCamera.transform.forward * directionMultiplier;
        setDirection.y = Mathf.Abs(setDirection.y) + 0.5f;
        float setPower = Mathf.Lerp(setForce * 0.5f, setForce * 2f, power / maxChargeTime);

        ballRb.isKinematic = false;
        ballRb.linearVelocity = setDirection.normalized * setPower;
    }

    void PerformSpike(float power)
    {
        if (!canSpike) return;

        Debug.Log("Spike realizado!");
        Debug.Log(power);

        if (ball != null)
        {
            RequestOwnership(ball);
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 spikeDirection = playerCamera.transform.forward + Vector3.down * 0.2f;
                spikeDirection = spikeDirection.normalized;

                ballRb.linearVelocity = Vector3.zero;

                float hitPower = spikeForce * (power / maxChargeTime + 0.1f);

                ballRb.AddForce(spikeDirection * hitPower, ForceMode.Impulse);

                Vector3 spin = playerCamera.transform.right * 20f;
                ballRb.AddTorque(spin, ForceMode.Impulse);
            }
        }

        canSpike = false;
    }



    void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 70f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleBallInteraction()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 20;
        PhotonNetwork.Instantiate(ballPrefab.name, spawnPosition, Quaternion.identity);
    }

    IEnumerator DelayedHitboxCheck(GameObject hitbox, Action action, float delay)
    {
        if (IsBallInHitbox(hitbox))
        {
            action.Invoke(); // Perform the action immediately
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < delay)
        {
            if (IsBallInHitbox(hitbox))
            {
                action.Invoke(); // Perform the action when the ball enters
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for next frame
        }
    }

    bool IsBallInHitbox(GameObject hitbox)
    {
        Collider[] hitColliders = Physics.OverlapBox(hitbox.transform.position, hitbox.transform.localScale / 2);
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject == ball)
            {
                Debug.Log("Collide With " + hitbox.name);
                return true; // Ball is inside
            }
        }
        return false;
    }

    public void SetBall(GameObject detectedBall)
    {
        canSet = true;
        ball = detectedBall;
    }

    public void ClearBall()
    {
        canSet = false;
        ball = null;
    }
    public void SpikeBall(GameObject detectedBall)
    {
        canSpike = true;
        ball = detectedBall;
    }

    public void ClearSpikeBall()
    {
        canSpike = false;
        ball = null;
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
}
