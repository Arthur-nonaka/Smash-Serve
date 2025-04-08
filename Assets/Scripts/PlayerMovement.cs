using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.VFX;
using TMPro;
using UnityEngine.InputSystem;

public enum Team
{
    None = 0,
    Team1 = 1,
    Team2 = 2
}

public enum ActionType
{
    SetFront = 0,
    SetBack = 1,
    Spike = 2
}

public class PlayerController : NetworkBehaviour
{
    private InputSystem_Actions inputActions;
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
    public float setForce = 8f;
    public Transform handPositionR;
    public Transform handPositionL;
    public float powerChargeSpeed = 3.0f;
    public float spikeForce = 10f;
    public float maxChargeTime = 0.001f;
    public float delayTime = 0.2f;
    public float durationOnAir = 0.7f;
    public GameObject ballPrefab;
    private Image powerCircle;
    private GameObject lastTouchedText;
    private Slider jumpSlider;

    private Vector3 velocity;
    private float chargeTime = 0f;
    private float chargeTimeReverse = 0f;
    private float hitChargeTime = 0f;
    private GameObject ball;
    private bool canSet = false;
    private bool canPerformSet = true;
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
    public float diveCooldown = 2f;
    private bool canDive = true;

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

    public bool isServing = false;

    private bool isPaused = false;

    [SyncVar]
    private string serverType = "Spin";

    [Header("Team Colors")]
    public Material team1Color;
    public Material team2Color;

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

        powerCircle = GameObject.FindGameObjectWithTag("powerSlider").GetComponent<Image>();
        jumpSlider = GameObject.FindGameObjectWithTag("jumpSlider").GetComponent<Slider>();



        jumpSlider.minValue = 0;
        jumpSlider.maxValue = 100;
        jumpSlider.value = 0;

        lastTouchedText = GameObject.FindGameObjectWithTag("LastTouch");
        lastTouchedText.SetActive(false);
        lastTouchedText.transform.Find("Image").gameObject.SetActive(false);

        UIManager.HideOptionsPanel();
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

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (ChatManager.Instance != null && ChatManager.Instance.IsChatActive())
        {
            return;
        }

        if (UIManager != null && UIManager.IsOptionsMenuActive())
        {
            return;
        }

        HandleMovement();
        if (!isDiving)
        {
            HandleCamera();
        }
        // HandleBallInteraction();


        if (Input.GetKeyDown(KeyCode.V) && !isServing && GetIsGrounded() && !isDiving)
        {
            CmdSpawnBallServe();
            isServing = true;
        }

        if (!isServing)
        {

            HandleJump();
            HandleSpike();
            HandleBlock();
            HandleDive();

            float currentY = transform.position.y;
            animator.SetFloat("Height", currentY);

            if (Input.GetKey(KeybindManager.Instance.GetKey("Front_Set")) && !Input.GetKey(KeybindManager.Instance.GetKey("Back_Set")))
            {
                hitChargeTime += Time.deltaTime * powerChargeSpeed;
                hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
                animator.SetBool("IsSetting", true);

                float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
                powerCircle.fillAmount = powerPercent / 100f;
            }

            if (Input.GetKeyUp(KeybindManager.Instance.GetKey("Front_Set")))
            {
                StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsSetting", true, 0.6f));
                float power = hitChargeTime;
                NetworkIdentity parentIdentity = GetComponentInParent<NetworkIdentity>();
                uint parentNetId = parentIdentity.netId;
                StartCoroutine(DelayedHitboxCheck(spikeHitbox, () => PerformSet(power, 1), () => PerformSet(power, 1), delayTime));
                hitChargeTime = 0f;
                powerCircle.fillAmount = 0f;
            }

            if (Input.GetKey(KeybindManager.Instance.GetKey("Back_Set")) && !Input.GetKey(KeybindManager.Instance.GetKey("Front_Set")))
            {
                hitChargeTime += Time.deltaTime * powerChargeSpeed;
                hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);
                animator.SetBool("IsSetting", true);

                float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
                powerCircle.fillAmount = powerPercent / 100f;
            }

            if (Input.GetKeyUp(KeybindManager.Instance.GetKey("Back_Set")))
            {
                StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsSetting", true, 0.6f));
                float power = hitChargeTime;
                NetworkIdentity parentIdentity = GetComponentInParent<NetworkIdentity>();
                uint parentNetId = parentIdentity.netId;
                StartCoroutine(DelayedHitboxCheck(spikeHitbox, () => PerformSet(power, -1), () => PerformSet(power, -1), delayTime));
                hitChargeTime = 0f;
                powerCircle.fillAmount = 0f;
            }
        }
        else
        {
            if (Input.GetMouseButton(1))
            {
                hitChargeTime += Time.deltaTime * powerChargeSpeed;
                hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

                float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
                powerCircle.fillAmount = powerPercent / 100f;
            }

            if (Input.GetMouseButtonUp(1))
            {
                float power = hitChargeTime;
                CmdServeBall(power);
                hitChargeTime = 0f;
                powerCircle.fillAmount = 0f;
                isServing = false;
                CmdSetServerType("Spin");
            }

            if (Input.GetMouseButton(0))
            {
                hitChargeTime += Time.deltaTime * powerChargeSpeed;
                hitChargeTime = Mathf.Clamp(hitChargeTime, 0, maxChargeTime);

                float powerPercent = (hitChargeTime / maxChargeTime) * 100f;
                powerCircle.fillAmount = powerPercent / 100f;
            }

            if (Input.GetMouseButtonUp(0))
            {
                float power = hitChargeTime;
                CmdServeBall(power);
                hitChargeTime = 0f;
                powerCircle.fillAmount = 0f;
                isServing = false;
                CmdSetServerType("Float");
            }

        }

    }

    [Command]
    void CmdSetServerType(string type)
    {
        serverType = type;
    }

    [Command]
    void CmdSpawnBallServe()
    {
        if (TeamManager.Instance.matchActive)
        {
            if (TeamManager.Instance.designatedServerNetId != netId && TeamManager.Instance.designatedServerNetId != 0)
            {
                Debug.Log("Only the designated server can spawn the ball.");
                return;
            }
            if (FindObjectOfType<VolleyballBall>() != null)
            {
                Debug.Log("A ball already exists in the scene. Cannot spawn another one.");
                return;
            }

            TeamManager.Instance.currentTouches = 2;
        }

        Vector3 spawnPos = handPositionR.position;
        GameObject ballInstance = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

        BallController ballCtrl = ballInstance.GetComponent<BallController>();
        if (ballCtrl != null)
        {
            ballCtrl.AttachToHand(handPositionR);
        }
        else
        {
            Debug.LogError("Ball prefab is missing the BallController component.");
        }

        NetworkServer.Spawn(ballInstance);

        ball = ballInstance;
        isServing = true;
    }

    [Command]
    void CmdServeBall(float charge)
    {
        if (ball != null)
        {
            BallController ballCtrl = ball.GetComponent<BallController>();
            if (ballCtrl != null)
            {
                Vector3 serveForce = transform.forward * (charge * 0.7f) + Vector3.up * (charge * 1.5f + 4f);
                ballCtrl.Serve(serveForce);
            }
            else
            {
                Debug.LogError("Ball does not have a BallController component.");
            }

            ball = null;
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
        if (Input.GetKey(KeybindManager.Instance.GetKey("Dive")) && GetIsGrounded() && !isDiving && canDive)
        {
            StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsDiving", true, 0.5f));
            diveHitbox.SetActive(true);
            isDiving = true;
            canDive = false;
            StartCoroutine(EndDiveAfterDelay(diveDuration));
            StartCoroutine(DiveCooldown());
            CmdDive();
        }
    }

    private IEnumerator DiveCooldown()
    {
        yield return new WaitForSeconds(diveCooldown);
        canDive = true;
    }

    [Command]
    void CmdDive()
    {
        if (new System.Random().Next(0, 10) <= 6)
        {
            RpcPlayTennisSound(GetComponent<NetworkIdentity>().netId);
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


        blockHardness = Mathf.Lerp(0.2f, 1.2f, t);

        if (!blockHitbox.activeSelf)
            blockHitbox.SetActive(true);

    }

    [Command]
    void CmdActivateBlock()
    {
        RpcActivateBlock();
    }

    [Command]
    public void CmdDeactivateBlock()
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
            powerCircle.fillAmount = powerPercent / 100f;
            isAttacking = true;
        }

        if (Input.GetMouseButtonUp(0) && !GetIsGrounded())
        {
            float power = hitChargeTime;
            NetworkIdentity parentIdentity = GetComponentInParent<NetworkIdentity>();
            uint parentNetId = parentIdentity.netId;
            StartCoroutine(DelayedHitboxCheck(spikeHitbox, () => PerformImmediateSpike(power), () => PerformDelayedSpike(power), delayTime));
            hitChargeTime = 0f;
            powerCircle.fillAmount = 0f;
            isAttacking = false;
            virtualMousePos = Vector2.zero;
        }

        if (Input.GetMouseButtonUp(0) && GetIsGrounded() && isAttacking)
        {
            hitChargeTime = 0f;
            powerCircle.fillAmount = 0f;
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
        if (moveZ > 0 && Input.GetKey(KeybindManager.Instance.GetKey("Sprint")))
        {
            animator.SetFloat("Vertical", moveZ + 1);
        }

        float speed = walkSpeed;
        Vector3 finalMove;

        bool wasWalking = horizontalVelocity.sqrMagnitude > 0;

        if (GetIsGrounded())
        {
            speed = (Input.GetKey(KeyCode.W) && Input.GetKey(KeybindManager.Instance.GetKey("Sprint")) && !Input.GetKey(KeybindManager.Instance.GetKey("Jump"))) ? runSpeed : walkSpeed;
            if (Input.GetMouseButton(1) && !isServing)
            {
                speed *= 0.5f;
                animator.SetBool("IsBumping", true);
            }
            else
            {
                animator.SetBool("IsBumping", false);
            }
            if (Input.GetKey(KeybindManager.Instance.GetKey("Jump")))
            {
                animator.SetBool("IsBumping", false);
                speed *= 0.5f;
            }
        }
        lockedHorizontalVelocity = worldInput * speed;
        horizontalVelocity = lockedHorizontalVelocity;
        finalMove = horizontalVelocity;

        bool isWalking = horizontalVelocity.sqrMagnitude > 0;

        if (!wasWalking && isWalking)
        {
            CmdPlayWalkSound();
        }

        if (!isDiving)
        {
            controller.Move(finalMove * Time.deltaTime);
        }
        else
        {
            controller.Move(transform.forward * diveForce * Time.deltaTime);
        }
    }

    [Command]
    void CmdPlayWalkSound()
    {
        if (new System.Random().Next(0, 10) <= 3)
        {
            RpcPlayTennisSound(GetComponent<NetworkIdentity>().netId);
        }
    }

    void HandleJump()
    {
        if (Input.GetKey(KeybindManager.Instance.GetKey("Jump")) && GetIsGrounded() && !stopJump && !isDiving)
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

        if (Input.GetKeyUp(KeybindManager.Instance.GetKey("Jump")) && GetIsGrounded() && !isDiving)
        {
            if (!stopJump)
            {
                StartCoroutine(animationController.SetAnimatorBoolWithDelay("IsJumping", true, 0.5f));
                float jumpPower = jumpForce * (chargeTime / maxChargeTime + 0.5f);
                float charged = chargeTime;
                CmdPerformJump(charged);
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
    public void CmdPerformJump(float chargeTime)
    {
        vfxManager.GetComponent<VFXManager>().RpcPlayJumpVFX(transform.position + Vector3.up * 1.2f);
        if ((chargeTime / Mathf.Abs(maxChargeTime)) * 100f > 70)
        {
            RpcPlayTennisSound(GetComponent<NetworkIdentity>().netId);
        }
    }

    public bool GetIsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, 0.3f);
    }

    void PerformSet(float power, int directionMultiplier)
    {
        if (!canSet || !canPerformSet || !CanTouch()) return;

        if (ball != null)
        {
            canPerformSet = false;
            StartCoroutine(ResetSetCooldown(0.5f));
            Vector3 setDirection = playerCamera.forward;
            if (directionMultiplier < 0)
            {
                setDirection = new Vector3(-setDirection.x, setDirection.y, -setDirection.z);
            }

            CmdPerformSet(power, directionMultiplier, setDirection);
            CmdNotifyBallTouched(false);
        }
    }


    IEnumerator ResetSetCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        canPerformSet = true;
    }

    [Command]
    void CmdPerformSet(float power, int directionMultiplier, Vector3 setDirection)
    {
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                if (!IsSameSide(ball))
                {
                    return;
                }

                NetworkIdentity identity = ball.GetComponent<NetworkIdentity>();
                if (ballRb.linearVelocity.magnitude > 15f)
                {
                    RpcPlayBumpSound(identity.netId);
                }
                else
                {
                    RpcPlaySetSound(identity.netId);
                }
                StartCoroutine(ServerHoldAndSetBall(ballRb, power, directionMultiplier, setDirection, ballRb.linearVelocity));
            }
        }
    }

    IEnumerator ServerHoldAndSetBall(Rigidbody ballRb, float power, int directionMultiplier, Vector3 setDirection, Vector3 incomingVelocity)
    {
        Vector3 handMidPoint = (handPositionL.position + handPositionR.position) / 2;
        Vector3 targetPosition = handMidPoint + Vector3.back * 0.15f;

        ballRb.isKinematic = true;

        float duration = 0.15f;

        if (incomingVelocity.magnitude > 15f)
        {
            duration = 0.04f;
        }


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
        float setPower = Mathf.Lerp(setForce * 0.5f, setForce * 1.6f, power / maxChargeTime);

        ballRb.isKinematic = false;

        Vector3 finalVector;

        if (incomingVelocity.magnitude > 15f)
        {
            float slowFactor = 0.2f;
            Vector3 slowedIncoming = incomingVelocity * slowFactor;

            Vector3 setVector = setDirection.normalized * setPower;

            finalVector = slowedIncoming + setVector + Vector3.up * 2f;
        }
        else
        {
            finalVector = setDirection.normalized * setPower;
        }

        ballRb.AddForce(finalVector, ForceMode.Impulse);

        RpcSyncBallState(ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);

        yield return new WaitForFixedUpdate();

        ballRb.GetComponentInChildren<TrajectoryDrawer>().TriggerTrajectoryDisplay();

        yield return null;
    }

    [ClientRpc]
    void RpcPlaySetAnimation(string animationName)
    {
        StartCoroutine(animationController.SetAnimatorBoolWithDelay(animationName, true, 0.5f));
        animator.SetBool("IsSetting", false);
    }

    void PerformImmediateSpike(float power)
    {
        if (!canSpike || !CanTouch()) return;

        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                if (!IsSameSide(ball))
                {
                    return;
                }

                Vector3 spikeDirection = (playerCamera.forward + Vector3.down * 0.2f).normalized;
                float hitPower = spikeForce * (power / maxChargeTime + 0.2f);

                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Spiked", true, 0.5f));
                animator.SetBool("IsSpiking", false);
                CmdPerformSpike(spikeDirection, hitPower, "Immediate");
                CmdNotifyBallTouched(false);
                Debug.Log("Immediate spike attempted, command sent to server.");
            }
        }
        canSpike = false;
        isAttacking = false;
        virtualMousePos = Vector2.zero;
    }

    void PerformDelayedSpike(float power)
    {
        if (!canSpike || !CanTouch()) return;

        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                if (!IsSameSide(ball))
                {
                    return;
                }

                Vector3 spikeDirection = (playerCamera.forward + Vector3.up * 0.6f).normalized;
                float hitPower = spikeForce * (power / maxChargeTime - 0.2f);

                StartCoroutine(animationController.SetAnimatorBoolWithDelay("Spiked", true, 0.5f));
                animator.SetBool("IsSpiking", false);
                CmdPerformSpike(spikeDirection, hitPower, "Delayed");
                CmdNotifyBallTouched(false);
                Debug.Log("Delayed spike attempted, command sent to server.");
            }
        }
        canSpike = false;
        isAttacking = false;
        virtualMousePos = Vector2.zero;
    }

    [Command]
    void CmdPerformSpike(Vector3 spikeDirection, float hitPower, string sound)
    {
        if (ball != null)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                vfxManager.GetComponent<VFXManager>().RpcPlaySpikeVFX(ball.transform.position);
                NetworkIdentity identity = ball.GetComponent<NetworkIdentity>();
                Vector3 spin;
                if (sound == "Immediate")
                {
                    RpcPlaySpikeSound(identity.netId, hitPower);
                    spin = playerCamera.right * 20f;
                }
                else
                {
                    RpcPlayDelayedSpikeSound(identity.netId, hitPower);
                    spin = playerCamera.right * 0.2f;
                }

                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
                ballRb.AddForce(spikeDirection * hitPower, ForceMode.Impulse);
                if (serverType == "Spin")
                {
                    ballRb.AddTorque(spin, ForceMode.Impulse);
                }
                else
                {
                    ball.GetComponent<VolleyballBall>().Float();
                }
                serverType = "Spin";

                ballRb.GetComponentInChildren<TrajectoryDrawer>().TriggerTrajectoryDisable();
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
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 70f);

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

    [Command]
    public void CmdNotifyBallTouched(bool isBlock)
    {
        Debug.Log($"CmdNotifyBallTouched called by player {netId}, isBlock: {isBlock}");
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
        else
        {
            Debug.LogError("TeamManager.Instance is null!");
        }
    }

    IEnumerator DelayedHitboxCheck(GameObject hitbox, Action immediateAction, Action delayedAction, float delay)
    {
        if (IsBallCenterInHitbox(hitbox))
        {
            immediateAction.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < delay)
        {
            if (IsBallCenterInHitbox(hitbox))
            {
                delayedAction.Invoke();
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    bool IsBallCenterInHitbox(GameObject hitbox)
    {
        Collider hitboxCollider = hitbox.GetComponent<Collider>();
        if (hitboxCollider == null || ball == null)
            return false;

        return hitboxCollider.bounds.Contains(ball.transform.position);
    }

    [ClientRpc]
    void RpcPlayTennisSound(uint audioSourceNetId)
    {
        NetworkIdentity identity = NetworkClient.spawned[audioSourceNetId];
        if (identity != null)
        {
            AudioSource audioSource = identity.GetComponent<AudioSource>();
            if (audioSource != null && SoundManager.Instance.tennisSound.Length > 0)
            {
                int randomNumber = new System.Random().Next(0, SoundManager.Instance.tennisSound.Length);
                SoundManager.Instance.PlaySound(SoundManager.Instance.tennisSound[randomNumber], audioSource);
            }
            else
            {
                Debug.LogError("AudioSource not found or tennisSound array is empty." + SoundManager.Instance.tennisSound.Length);
            }
        }
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
    void RpcPlayBumpSound(uint audioSourceNetId)
    {
        NetworkIdentity identity = NetworkClient.spawned[audioSourceNetId];
        AudioSource audioSource = GetComponent<AudioSource>();
        SoundManager.Instance.PlaySound(SoundManager.Instance.bumpSound, audioSource);
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
    void RpcPlayDelayedSpikeSound(uint audioSourceNetId, float hitPower)
    {
        float volume = Mathf.Clamp(hitPower / 20f, 0.4f, 1f);

        NetworkIdentity identity = NetworkClient.spawned[audioSourceNetId];
        if (identity != null)
        {
            AudioSource audioSource = identity.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = volume;
                SoundManager.Instance.PlaySound(SoundManager.Instance.bumpSound, audioSource);
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

    public bool IsSameSide(GameObject ball)
    {
        float playerX = transform.position.x;
        float ballX = ball.transform.position.x;
        return
            (playerX <= 0 && ballX <= 0) ||
            (playerX >= 0 && ballX >= 0);
    }


    public bool CanTouch()
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogWarning("TeamManager reference is missing.");
            return false;
        }

        if (TeamManager.Instance.matchActive == false)
        {
            return true;
        }

        return TeamManager.Instance.lastTouchPlayerNetId != netId;
    }


    [ClientRpc]
    public void RpcShowLastTouchedMessage()
    {
        if (lastTouchedText != null)
        {
            lastTouchedText.transform.Find("Image").gameObject.SetActive(true);
            StartCoroutine(HideLastTouchedMessageAfterDelay(2f));
        }
    }

    private IEnumerator HideLastTouchedMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (lastTouchedText != null)
        {
            lastTouchedText.transform.Find("Image").gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void RpcHideLastTouchedMessage()
    {
        if (lastTouchedText != null)
        {
            lastTouchedText.transform.Find("Image").gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void RpcSetTeamColor(int teamId)
    {
        if (teamId == 1)
        {
            GetComponentsInChildren<Renderer>()[1].material = team1Color;
        }
        else if (teamId == 2)
        {
            GetComponentsInChildren<Renderer>()[1].material = team2Color;
        }

    }
}