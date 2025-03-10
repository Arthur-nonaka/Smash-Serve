using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.VFX;
public enum Team
{
    None,
    Team1,
    Team2
}

public class PlayerController : NetworkBehaviour
{
    public CharacterController controller;
    public Transform playerCamera;

    public Animator animator;
    public AnimationController animationController;

    public GameObject setHitbox;
    public GameObject spikeHitbox;
    public GameObject blockHitbox;
    public GameObject blockHitboxPivot;
    public GameObject diveHitbox;
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

    private bool wasBlocking = false;

    [Header("Dive Settings")]
    public float diveForce = 10f;
    public float diveDuration = 1.0f;
    private bool isDiving = false;

    private PlayerNameTag playerNameTag;

    [Header("VFX Settings")]
    private GameObject vfxManager;

    [Header("Team")]
    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team team = Team.None;

    private UIManager UIManager;

    [Header("Block Settings")]
    public PhysicsMaterial softMaterial;
    public PhysicsMaterial hardMaterial;

    public float blockHardness { get; private set; }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        virtualMousePos = Vector2.zero;
        int playerLayer = LayerMask.NameToLayer("Player");
        int ballLayer = LayerMask.NameToLayer("Ball");

        if (playerNameTag == null)
            playerNameTag = GetComponentInChildren<PlayerNameTag>();

        blockHitbox.SetActive(false);

        if (playerLayer == -1 || ballLayer == -1)
        {
            Debug.LogError("Player or Ball layer not found. Please ensure the layers are correctly set up.");
        }
        else
        {
            Physics.IgnoreLayerCollision(playerLayer, ballLayer);
        }

        if (!isLocalPlayer)
        {
            enabled = false;
            playerCamera.gameObject.SetActive(false);
        }

        virtualJoystickUI = FindObjectOfType<VirtualJoystickUI>();
        diveHitbox.SetActive(false);

        vfxManager = GameObject.FindGameObjectWithTag("VFXManager");
        UIManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();

        powerSlider = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Slider>();
        jumpSlider = GameObject.FindGameObjectWithTag("jumpSlider").GetComponent<Slider>();

        powerSlider.minValue = 0;
        powerSlider.maxValue = 100;
        powerSlider.value = 0;

        jumpSlider.minValue = 0;
        jumpSlider.maxValue = 100;
        jumpSlider.value = 0;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        TeamManager.Instance.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        TeamManager.Instance.UnregisterPlayer(this);
    }

    [Command]
    public void CmdSetTeam(Team newTeam)
    {
        team = newTeam;
        TeamManager.Instance.SetPlayerTeam(this, team);
        TargetShowMessage(connectionToClient, $"You are now on {team}.", Color.gray);
        TargetHideTeamsPanel(connectionToClient);
        Debug.Log($"Player {netId} set to team {team}");
    }

    [TargetRpc]
    void TargetHideTeamsPanel(NetworkConnection target)
    {

        UIManager.HideTeamsPanel();
    }

    void OnTeamChanged(Team oldTeam, Team newTeam)
    {
        Debug.Log($"Team changed from {oldTeam} to {newTeam}");
    }

    [TargetRpc]
    void TargetShowMessage(NetworkConnection target, string message, Color color)
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.QueueNotification(message, color);
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        HandleMovement();
        HandleJump();
        HandleCamera();
        // HandleBallInteraction();
        HandleSpike();
        HandleBlock();
        HandleDive();

        float currentY = transform.position.y;
        animator.SetFloat("Height", currentY);

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

    void HandleDive()
    {
        if (Input.GetKey(KeyCode.Q) && GetIsGrounded() && !isDiving)
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsDiving", true, 0.5f));
            diveHitbox.SetActive(true);
            isDiving = true;
            StartCoroutine(EndDiveAfterDelay(diveDuration));
        }
    }

    IEnumerator EndDiveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("IsDiving", false);
        diveHitbox.SetActive(false);
        isDiving = false;
    }

    void HandleBlock()
    {
        if (Input.GetMouseButton(1) && !GetIsGrounded() && !Input.GetMouseButton(0))
        {
            CmdActivateBlock();
            animator.SetBool("IsBlocking", true);
            wasBlocking = true;
            UpdateBlockProperties();
        }
        if ((Input.GetMouseButtonUp(1) || GetIsGrounded()) && wasBlocking)
        {
            CmdDeactivateBlock();
            animator.SetBool("IsBlocking", false);
            blockHitbox.SetActive(false);
            wasBlocking = false;
        }
    }

    void UpdateBlockProperties()
    {
        if (blockHitbox == null || playerCamera == null)
            return;

        Collider blockCollider = blockHitbox.GetComponent<Collider>();
        if (blockCollider != null)
        {
            blockCollider.material = blockHardness < 1.0f ? softMaterial : hardMaterial;
        }



        float pitch = playerCamera.eulerAngles.x;
        if (pitch > 180f)
            pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -30f, 30f);

        float maxScale = 1.2f;
        float minScale = 0.7f;
        float t = Mathf.InverseLerp(-30f, 30f, pitch);
        float newScale = Mathf.Lerp(maxScale, minScale, t);
        blockHitbox.transform.localScale = new Vector3(newScale, 1.6f, 0.3f);

        blockHitboxPivot.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);


        blockHardness = Mathf.Lerp(0.5f, 1.5f, t);

        if (!blockHitbox.activeSelf)
            blockHitbox.SetActive(true);

    }

    [Command]
    void CmdActivateBlock()
    {
        RpcActivateBlock();
    }

    [Command]
    void CmdDeactivateBlock()
    {
        RpcDeactivateBlock();
    }

    [ClientRpc]
    void RpcActivateBlock()
    {
        if (blockHitbox != null)
        {
            blockHitbox.SetActive(true);
        }
    }

    [ClientRpc]
    void RpcDeactivateBlock()
    {
        if (blockHitbox != null)
        {
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

        if (!isDiving)
        {
            controller.Move(finalMove * Time.deltaTime);
        }
        else
        {
            controller.Move(transform.forward * diveForce * Time.deltaTime);
        }
    }

    void HandleJump()
    {
        if (Input.GetKey(KeyCode.Space) && GetIsGrounded() && !stopJump && !isDiving)
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

        if (Input.GetKeyUp(KeyCode.Space) && GetIsGrounded() && !isDiving)
        {
            if (!stopJump)
            {
                CmdPerformJump();
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

    [Command]
    public void CmdPerformJump()
    {
        vfxManager.GetComponent<VFXManager>().RpcPlayJumpVFX(transform.position + Vector3.up * 1.2f);
    }

    public bool GetIsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, 0.3f);
    }

    void PerformSet(float power, int directionMultiplier)
    {
        if (!canSet) return;

        if (ball != null)
        {
            Vector3 setDirection = playerCamera.forward;
            if (directionMultiplier < 0)
            {
                setDirection = new Vector3(-setDirection.x, setDirection.y, -setDirection.z);
            }

            CmdPerformSet(power, directionMultiplier, setDirection);
        }
    }

    [Command]
    void CmdPerformSet(float power, int directionMultiplier, Vector3 setDirection)
    {
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                CmdNotifyBallTouched(false);
                NetworkIdentity identity = ball.GetComponent<NetworkIdentity>();
                RpcPlaySetSound(identity.netId);
                StartCoroutine(ServerHoldAndSetBall(ballRb, power, directionMultiplier, setDirection));
            }
        }
    }

    IEnumerator ServerHoldAndSetBall(Rigidbody ballRb, float power, int directionMultiplier, Vector3 setDirection)
    {
        Vector3 handMidPoint = (handPositionL.position + handPositionR.position) / 2;
        Vector3 targetPosition = handMidPoint + Vector3.back * 0.15f;

        ballRb.isKinematic = true;

        float duration = 0.16f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            Vector3 currentHandMidPoint = (handPositionL.position + handPositionR.position) / 2;
            Vector3 newTarget = currentHandMidPoint + Vector3.back * 0.15f;
            ball.transform.position = Vector3.Lerp(ball.transform.position, newTarget, Time.deltaTime * 10f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (directionMultiplier >= 1)
        {
            RpcPlaySetAnimation("Front_Set");
        }
        else
        {
            RpcPlaySetAnimation("Back_Set");
        }

        float setPower = Mathf.Lerp(setForce * 0.5f, setForce * 2f, power / maxChargeTime);

        ballRb.isKinematic = false;

        ballRb.AddForce(setDirection.normalized * setPower, ForceMode.Impulse);

        RpcSyncBallState(ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);
        yield return null;
    }

    [ClientRpc]
    void RpcPlaySetAnimation(string animationName)
    {
        StartCoroutine(animationController.SetAnimatorBoolWithDelay(animationName, true, 0.5f));
        animator.SetBool("IsSetting", false);
    }

    void PerformSpike(float power)
    {
        if (!canSpike) return;

        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                Vector3 spikeDirection = (playerCamera.forward + Vector3.down * 0.2f).normalized;
                float hitPower = spikeForce * (power / maxChargeTime + 0.1f);

                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Spiked", true, 0.5f));
                animator.SetBool("IsSpiking", false);
                CmdPerformSpike(spikeDirection, hitPower);
                Debug.Log("Spike attempted, command sent to server.");
            }
        }
        canSpike = false;
        isAttacking = false;
        virtualMousePos = Vector2.zero;
    }

    [Command]
    void CmdPerformSpike(Vector3 spikeDirection, float hitPower)
    {
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                vfxManager.GetComponent<VFXManager>().RpcPlaySpikeVFX(ball.transform.position);
                CmdNotifyBallTouched(false);
                NetworkIdentity identity = ball.GetComponent<NetworkIdentity>();
                RpcPlaySpikeSound(identity.netId, hitPower);
                Vector3 spin = playerCamera.right * 20f;


                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
                ballRb.AddForce(spikeDirection * hitPower, ForceMode.Impulse);
                ballRb.AddTorque(spin, ForceMode.Impulse);

                StartCoroutine(SyncBallStateNextFrame());
            }
        }
    }

    IEnumerator SyncBallStateNextFrame()
    {
        yield return new WaitForFixedUpdate();
        RpcSyncBallState(ball.transform.position, ball.GetComponent<Rigidbody>().linearVelocity, ball.GetComponent<Rigidbody>().angularVelocity);
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
                    if (virtualJoystickUI != null)
                        virtualJoystickUI.virtualJoystickOffsetX = 0f;
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

    // void HandleBallInteraction()
    // {
    //     if (Input.GetKeyDown(KeyCode.B))
    //     {
    //         CmdSpawnBall();
    //     }
    // }

    // [Command]
    // void CmdSpawnBall()
    // {
    //     Vector3 spawnPosition = transform.position + Vector3.up * 20;
    //     GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
    //     NetworkServer.Spawn(newBall);
    // }

    [Command]
    public void CmdNotifyBallTouched(bool isBlock)
    {
        if (TeamManager.Instance != null)
        {
            if (!TeamManager.Instance.matchActive)
            {
                return;
            }

            if (isBlock)
            {
                TeamManager.Instance.UpdateBallLastTouchedBlock(this);
            }
            else
            {
                TeamManager.Instance.UpdateBallLastTouched(this);
            }
        }
    }

    IEnumerator DelayedHitboxCheck(GameObject hitbox, Action action, float delay)
    {
        if (IsBallInHitbox(hitbox))
        {
            action.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < delay)
        {
            if (IsBallCenterInHitbox(hitbox))
            {
                action.Invoke();
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
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
                return true;
            }
        }
        return false;
    }

    bool IsBallCenterInHitbox(GameObject hitbox)
    {
        Collider hitboxCollider = hitbox.GetComponent<Collider>();
        if (hitboxCollider == null || ball == null)
            return false;

        return hitboxCollider.bounds.Contains(ball.transform.position);
    }

    [ClientRpc]
    void RpcPlaySetSound(uint audioSourceNetId)
    {
        NetworkIdentity identity = NetworkClient.spawned[audioSourceNetId];
        if (identity != null)
        {
            AudioSource audioSource = identity.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                SoundManager.Instance.PlaySound(SoundManager.Instance.setSound, audioSource);
            }
        }
    }

    [ClientRpc]
    void RpcPlaySpikeSound(uint audioSourceNetId, float hitPower)
    {
        float volume = Mathf.Clamp(hitPower / 20f, 0.1f, 1f);

        NetworkIdentity identity = NetworkClient.spawned[audioSourceNetId];
        if (identity != null)
        {
            AudioSource audioSource = identity.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = volume;
                SoundManager.Instance.PlaySound(SoundManager.Instance.spikeSound, audioSource);
            }
        }
    }

    [ClientRpc]
    public void RpcTeleport(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public string GetPlayerName()
    {
        return playerNameTag.GetPlayerName();
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

}