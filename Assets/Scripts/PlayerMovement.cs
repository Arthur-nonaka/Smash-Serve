using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun
{
    public CharacterController controller;
    public Transform playerCamera;

    public Animator animator;
    public AnimationController animationController;

    public GameObject setHitbox;
    public GameObject spikeHitbox;
    public GameObject blockHitbox;
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
    public float delayTime = 0.2f;
    public float durationOnAir = 0.7f;
    public GameObject ballPrefab;
    private Slider powerSlider;
    private Slider jumpSlider;

    private Vector3 velocity;
    private float chargeTime = 0f;
    private float chargeTimeReverse = 0f;
    private float hitChargeTime = 0f;
    private GameObject ball;
    private bool canSet = false;
    private bool canSpike = false;
    private bool stopJump = false;

    private Quaternion lockedRotation;
    private Vector3 lockedHorizontalVelocity = Vector3.zero;
    private Vector3 horizontalVelocity = Vector3.zero;
    public float airAcceleration = 2f;
    public float airControlFactor = 0.3f;

    public float attackMouseSlowFactor = 0.5f;

    private bool isAttacking = false;
    public float cameraLerpSpeed = 5f;

    private bool wasGrounded = false;

    [Header("Sensitivity & Rotation")]
    public float mouseSensitivity = 2f;
    public float verticalRotation = 0f;
    private float cameraYaw = 0f;

    [Header("Attack Joystick Settings")]
    public float joystickMaxOffset = 50f;
    public float joystickDeadzone = 2f;
    public float joystickMaxRotationSpeed = 90f;
    private VirtualJoystickUI virtualJoystickUI;
    private Vector2 virtualMousePos;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        virtualMousePos = Vector2.zero;
        virtualJoystickUI = FindAnyObjectByType<VirtualJoystickUI>();
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
        HandleBlock();

        float currentY = transform.position.y;
        animator.SetFloat("Height", currentY);

        if (Input.GetKeyDown(KeyCode.V))
        {
            InstantiateVolleyballTowardsPlayer();
        }


        if (Input.GetKey(KeyCode.F) && !Input.GetKey(KeyCode.R))
        {

            hitChargeTime += Time.deltaTime * powerChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
            animator.SetBool("IsSetting", true);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsSetting", true, 0.6f));
            float power = hitChargeTime;
            StartCoroutine(DelayedHitboxCheck(setHitbox, () => PerformSet(power, 1), delayTime));
            hitChargeTime = 0f;

            powerSlider.value = 0;
        }

        if (Input.GetKey(KeyCode.R) && !Input.GetKey(KeyCode.F))
        {
            hitChargeTime += Time.deltaTime * powerChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
            animator.SetBool("IsSetting", true);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsSetting", true, 0.6f));
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

    void InstantiateVolleyballTowardsPlayer()
    {
        Vector3 spawnPosition = transform.position + transform.forward * 10f + Vector3.up * 7f;
        GameObject volleyball = PhotonNetwork.Instantiate(ballPrefab.name, spawnPosition, Quaternion.identity);
        Rigidbody volleyballRb = volleyball.GetComponent<Rigidbody>();
        if (volleyballRb != null)
        {
            Vector3 directionToPlayer = (transform.position - spawnPosition).normalized;
            volleyballRb.AddForce(directionToPlayer * 23f + Vector3.up * 8.5f, ForceMode.Impulse);
        }
    }

    void HandleBlock()
    {
        if (Input.GetMouseButton(1) && !GetIsGrounded() && !Input.GetMouseButton(0))
        {
            animator.SetBool("IsBlocking", true);
            blockHitbox.SetActive(true);

        }
        else
        {
            animator.SetBool("IsBlocking", false);
            blockHitbox.SetActive(false);
        }
    }

    void HandleSpike()
    {
        if (Input.GetMouseButton(0) && !GetIsGrounded() && !Input.GetMouseButton(1))
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsSpiking", true, 1.5f));
            hitChargeTime += Time.deltaTime * powerChargeSpeed;
            hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

            float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
            powerSlider.value = powerPercent;
            isAttacking = true;
        }

        if (Input.GetMouseButtonUp(0) && !GetIsGrounded())
        {
            float power = hitChargeTime;
            StartCoroutine(DelayedHitboxCheck(spikeHitbox, () => PerformSpike(power), delayTime));
            hitChargeTime = 0f;

            powerSlider.value = 0;
            isAttacking = false;
            virtualMousePos = Vector2.zero;
        }

        if (Input.GetMouseButtonUp(0) && GetIsGrounded() && isAttacking)
        {
            hitChargeTime = 0f;

            powerSlider.value = 0;
            isAttacking = false;
            virtualMousePos = Vector2.zero;
        }
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 inputVector = new Vector3(moveX, 0, moveZ);
        Vector3 worldInput = (transform.right * moveX + transform.forward * moveZ).normalized;

        animator.SetFloat("Horizontal", moveX);
        animator.SetFloat("Vertical", moveZ);
        animator.SetFloat("Speed", inputVector.sqrMagnitude);
        if (moveZ > 0 && Input.GetKey(KeyCode.LeftShift))
        {
            animator.SetFloat("Vertical", moveZ + 1);
        }

        float speed = walkSpeed;
        Vector3 finalMove;

        if (GetIsGrounded())
        {
            speed = (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space)) ? runSpeed : walkSpeed;
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
        lockedHorizontalVelocity = worldInput * speed;
        horizontalVelocity = lockedHorizontalVelocity;
        finalMove = horizontalVelocity;

        controller.Move(finalMove * Time.deltaTime);
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
            if (!stopJump)
            {
                StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsJumping", true, 0.5f));
                float jumpPower = jumpForce * (chargeTime / maxChargeTime + 0.5f);
                velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
                chargeTime = 0f;
                chargeTimeReverse = 0f;
                jumpSlider.value = 0;
                wasGrounded = false;
            }
            animator.SetBool("IsApproach", false);
            stopJump = false;
        }

        if (velocity.y > -1.5f && velocity.y < 0)
        {
            velocity.y *= durationOnAir;
        }


        velocity.y += gravity * Time.deltaTime;
        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
    }

    public bool GetIsGrounded()
    {
        return Physics.Raycast((transform.position + Vector3.up * 1f), Vector3.down, out RaycastHit hit, 0.29f);
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
        float duration = 0.16f;
        float elapsedTime = 0f;
        Vector3 handMidPoint = (handPositionL.position + handPositionR.position) / 2;
        Vector3 midpoint = new Vector3(handMidPoint.x, transform.position.y * -7.1f, handMidPoint.z);
        midpoint += Vector3.back * 0.15f;

        ballRb.isKinematic = true;

        while (elapsedTime < duration)
        {
            Vector3 currentHandMidpoint = (handPositionL.position + handPositionR.position) / 2;
            Vector3 targetPosition = currentHandMidpoint + Vector3.back * 0.15f;

            ball.transform.position = Vector3.Lerp(ball.transform.position, targetPosition, Time.deltaTime * 10f);

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

        animator.SetBool("IsSetting", false);

        // ball.transform.position = midpoint;

        // yield return new WaitForSeconds(0.005f);

        Vector3 setDirection = playerCamera.transform.forward * directionMultiplier;
        setDirection.y = Mathf.Abs(setDirection.y) + 0.5f;
        float setPower = Mathf.Lerp(setForce * 0.5f, setForce * 2f, power / maxChargeTime);

        ballRb.isKinematic = false;
        yield return null;
        ballRb.AddForce(setDirection.normalized * setPower, ForceMode.Impulse);
        photonView.RPC("SyncBallState", RpcTarget.Others, ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);
    }

    void PerformSpike(float power)
    {
        if (!canSpike) return;

        if (ball != null)
        {
            RequestOwnership(ball);
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 spikeDirection = (playerCamera.transform.forward + Vector3.down * 0.2f).normalized;
                float hitPower = spikeForce * (power / maxChargeTime + 0.1f);
                Vector3 spin = playerCamera.transform.right * 20f;

                StartCoroutine(WaitForOwnershipAndSpike(ballRb, spikeDirection, hitPower, spin));
                Debug.Log("Spike attempted, waiting for ownership...");
            }
        }
        canSpike = false;
        isAttacking = false;
        virtualMousePos = Vector2.zero;
    }

    IEnumerator WaitForOwnershipAndSpike(Rigidbody ballRb, Vector3 spikeDirection, float hitPower, Vector3 spin)
    {
        while (!ball.GetComponent<PhotonView>().IsMine)
        {
            yield return null;
        }

        StartCoroutine(animationController.SetAnimatorBoolWithDelay("Spiked", true, 0.5f));
        animator.SetBool("IsSpiking", false);

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.AddForce(spikeDirection * hitPower, ForceMode.Impulse);
        ballRb.AddTorque(spin, ForceMode.Impulse);

        photonView.RPC("SyncBallState", RpcTarget.Others, ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);
    }



    void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 60f);

        if (isAttacking)
        {
            if (Mathf.Abs(mouseX) < 0.01f)
            {
                float returnSpeed = 5f;
                virtualMousePos.x = Mathf.Lerp(virtualMousePos.x, 0f, Time.deltaTime * returnSpeed);
            }
            else
            {
                virtualMousePos.x += mouseX;
            }
            virtualMousePos.x = Mathf.Clamp(virtualMousePos.x, -joystickMaxOffset, joystickMaxOffset);
            if (Mathf.Abs(virtualMousePos.x) < joystickDeadzone)
                virtualMousePos.x = 0f;

            if (virtualJoystickUI != null)
            {
                virtualJoystickUI.virtualJoystickOffsetX = virtualMousePos.x;
            }
            else
            {
                virtualJoystickUI.virtualJoystickOffsetX = Vector2.zero.x;

            }

            float normalizedHorizontal = virtualMousePos.x / joystickMaxOffset;
            float rotationDeltaX = normalizedHorizontal * joystickMaxRotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * rotationDeltaX);
            playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
        else
        {
            virtualMousePos.x = 0f;
            cameraYaw += mouseX;

            if (GetIsGrounded())
            {
                if (!wasGrounded)
                {
                    // transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
                }
                else
                {
                    transform.Rotate(Vector3.up * mouseX);
                }

                playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
            else
            {
                transform.Rotate(Vector3.up * mouseX);
                playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }

        wasGrounded = GetIsGrounded();
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
}
