using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class VolleyballBall : NetworkBehaviour
{
    private struct BallState
    {
        public Vector3 position;
        public Vector3 velocity;

        public Vector3 angularVelocity;
        public float time;
    }

    private Queue<BallState> ballStateBuffer = new Queue<BallState>();
    public float rewindTimeWindow = 0.2f;
    public float minHitForce = 5f;
    public float maxHitForce = 20f;
    public float gravityScale = 1f;
    public float bounciness = 0.6f;

    public float radius = 0.2f;
    public float airDensity = 1.2f;
    public float magnusCoefficient = 0.1f;

    private Rigidbody rb;
    private bool markCreated = false;

    public Sprite shadowSprite;
    private GameObject shadow;
    private SpriteRenderer shadowRenderer;

    public GameObject markPrefab;

    [SyncVar(hook = nameof(OnLastTouchedPlayerChanged))]
    public string lastPlayerName = "";

    [SyncVar]
    public bool bumpOccurred = false;

    public bool isFloating = false;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        PhysicsMaterial bounceMaterial = new PhysicsMaterial();
        bounceMaterial.bounciness = bounciness;
        bounceMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        bounceMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        GetComponent<SphereCollider>().material = bounceMaterial;

        if (shadowSprite != null)
        {
            shadow = new GameObject("Shadow");
            shadow.transform.localPosition = Vector3.zero;

            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.sortingOrder = -1;
            Color col = Color.black;
            col.a = 0.7f;
            shadowRenderer.color = col;
            shadow.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        int ballLayer = LayerMask.NameToLayer("Ball");
        int netBlock = LayerMask.NameToLayer("NetBlock");
        Physics.IgnoreLayerCollision(netBlock, ballLayer);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (TeamManager.Instance != null)
        {
            TeamManager.Instance.RegisterBall(this);
        }
    }

    void Update()
    {
        UpdateShadow();
    }

    void FixedUpdate()
    {
        if (!isServer) return;
        RecordBallState();
        ApplyMagnusEffect();
    }

    [Server]
    void RecordBallState()
    {
        BallState state = new BallState
        {
            position = transform.position,
            velocity = rb.linearVelocity,
            angularVelocity = rb.angularVelocity,
            time = Time.time
        };
        ballStateBuffer.Enqueue(state);


        while (ballStateBuffer.Count > 100)
        {
            ballStateBuffer.Dequeue();
        }
    }

    [ClientCallback]
    void UpdateShadow()
    {
        if (shadow == null) return;

        int layerMask = ~(
            LayerMask.GetMask("Ball") |
            LayerMask.GetMask("Player") |
            LayerMask.GetMask("Hitbox") |
            LayerMask.GetMask("Net") |
            LayerMask.GetMask("Block")
        );

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            shadow.transform.position = Vector3.Lerp(shadow.transform.position, hit.point + Vector3.up * 0.05f, Time.deltaTime * 10f);
        }

        float height = transform.position.y;
        float maxShadowSize = 30f;
        float minShadowSize = 1f;
        float maxHeight = 100f;

        float scale = Mathf.Lerp(minShadowSize, maxShadowSize, height / maxHeight);
        shadow.transform.localScale = new Vector3(scale, scale, 1);

        shadow.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
    void ApplyMagnusEffect()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 angularVelocity = rb.angularVelocity;

        if (angularVelocity == Vector3.zero && isFloating)
        {
            Debug.Log("Floating");
            float randomForce = UnityEngine.Random.Range(-0.4f, 0.4f);
            float randomForceVertical = UnityEngine.Random.Range(-0.4f, 0.4f);
            rb.AddForce(new Vector3(randomForce, randomForceVertical, randomForce), ForceMode.Impulse);
        }
        else
        {
            Vector3 magnusForce = magnusCoefficient * airDensity * Mathf.PI * Mathf.Pow(radius, 3) * Vector3.Cross(angularVelocity, velocity);
            rb.AddForce(magnusForce);
        }
    }

    public IEnumerator DisableFloatingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isFloating = false;
    }

    public void Float()
    {
        isFloating = true;
        StartCoroutine(DisableFloatingAfterDelay(1.5f));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        if (collision.gameObject.CompareTag("Block"))
        {
            PlayerController playerController = collision.gameObject.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                ContactPoint contact = collision.contacts[0];
                Vector3 impactPoint = contact.point;

                Vector3 blockCenter = collision.collider.bounds.center;
                Vector3 impactOffset = impactPoint - blockCenter;
                Vector3 extents = collision.collider.bounds.extents;
                float normalizedDistanceX = Mathf.Abs(impactOffset.x) / extents.x;
                float normalizedDistanceZ = Mathf.Abs(impactOffset.z) / extents.z;
                float edgeFactor = Mathf.Max(normalizedDistanceX, normalizedDistanceZ);
                edgeFactor = Mathf.Clamp01(edgeFactor * 1.2f);

                Vector3 originalDirection = -rb.linearVelocity.normalized;
                float incomingSpeed = rb.linearVelocity.magnitude;
                float spikePowerFactor = Mathf.Clamp(incomingSpeed / 20f, 0.5f, 1.5f);

                float hardnessFactor = Mathf.Lerp(0.2f, 1.0f, playerController.blockHardness);

                float baseBreakChance = 0.0f;

                if (playerController.blockHardness < 0.4f)
                {
                    baseBreakChance = 0.5f;
                }
                else if (playerController.blockHardness < 0.7f)
                {
                    baseBreakChance = 0.3f;
                }
                else
                {
                    baseBreakChance = 0.05f;
                }

                float powerBonus = (spikePowerFactor - 1.0f) * 0.3f;

                float edgeBonus = edgeFactor * 0.2f;

                float breakProbability = baseBreakChance + powerBonus + edgeBonus;
                breakProbability = Mathf.Clamp01(breakProbability);

                bool blockBreaks = UnityEngine.Random.value < breakProbability;

                Debug.Log("playerController.blockHardness: " + playerController.blockHardness.ToString("F2"));

                Debug.Log("Break chance: " + breakProbability.ToString("F2") +
                          " (base: " + baseBreakChance.ToString("F2") +
                          ", power: " + powerBonus.ToString("F2") +
                          ", edge: " + edgeBonus.ToString("F2") +
                          ") - Will break: " + blockBreaks);

                rb.linearVelocity *= hardnessFactor;

                if (blockBreaks)
                {
                    Debug.Log("Pass through");

                    Vector3 passDirection = originalDirection;
                    passDirection = Quaternion.Euler(
                        UnityEngine.Random.Range(-15f, 15f) * (1 - hardnessFactor),
                        UnityEngine.Random.Range(-15f, 15f) * (1 - hardnessFactor),
                        0
                    ) * passDirection;

                    passDirection += Vector3.up * 0.2f * (1 - hardnessFactor);
                    passDirection = passDirection.normalized;

                    float exitSpeedFactor = Mathf.Lerp(0.5f, 0.9f, 1 - hardnessFactor) * spikePowerFactor;
                    rb.linearVelocity = passDirection * incomingSpeed * exitSpeedFactor;

                    float randomAngle = UnityEngine.Random.Range(-30f, 30f) * (1 - hardnessFactor);
                    Vector3 randomDir = Quaternion.Euler(0, randomAngle, 0) * originalDirection;

                    float upwardForce = Mathf.Lerp(0.2f, 0.6f, spikePowerFactor) * (1 - hardnessFactor);
                    passDirection += Vector3.up * upwardForce;
                    passDirection = passDirection.normalized;

                    Physics.IgnoreCollision(collision.collider, GetComponent<Collider>(), true);
                    StartCoroutine(ReenableCollision(collision.collider, GetComponent<Collider>()));

                    rb.linearVelocity = passDirection * incomingSpeed * exitSpeedFactor;
                }
                else
                {
                    Vector3 bounceDir = rb.linearVelocity.normalized;
                    float bounceRandomness = (1 - hardnessFactor) * 0.3f;
                    bounceDir = Quaternion.Euler(
                        UnityEngine.Random.Range(-15f, 15f) * bounceRandomness,
                        UnityEngine.Random.Range(-15f, 15f) * bounceRandomness,
                        0
                    ) * bounceDir;

                    rb.linearVelocity = bounceDir * rb.linearVelocity.magnitude;
                }

                RpcPlayBumpSound();
            }
        }

        if (collision.gameObject.CompareTag("Ground") && !markCreated)
        {
            RpcPlayGroundSound();
            StartCoroutine(DelayedGroundCollisionResponse(collision));
        }
    }

    private IEnumerator ReenableCollision(Collider blockCollider, Collider ballCollider)
    {
        yield return new WaitForSeconds(0.2f);
        if (blockCollider != null && ballCollider != null)
        {
            Physics.IgnoreCollision(blockCollider, ballCollider, false);
        }
    }

    IEnumerator DelayedGroundCollisionResponse(Collision collision)
    {
        Vector3 hitPoint = collision.contacts[0].point;
        Collider courtCollider = GameObject.FindGameObjectWithTag("Court").GetComponent<Collider>();
        bool isIn = courtCollider.bounds.Contains(hitPoint);
        Color markColor = isIn ? Color.green : Color.red;
        yield return new WaitForSeconds(0.2f);

        if (bumpOccurred)
        {
            bumpOccurred = false;
            yield break;
        }

        if (isIn)
        {
            if (hitPoint.x > 0)
            {
                TeamManager.Instance.BallFellOnGround(Team.Team1);
            }
            else
            {
                TeamManager.Instance.BallFellOnGround(Team.Team2);
            }
        }
        else
        {
            TeamManager.Instance.BallFellOnGround(Team.None);
        }




        CreateMark(hitPoint, markColor);
        markCreated = true;
        GetComponent<Collider>().enabled = false;
    }

    [Server]
    void CreateMark(Vector3 position, Color color)
    {
        if (markPrefab == null) return;

        GameObject mark = Instantiate(markPrefab, position, Quaternion.identity);

        if (!mark.GetComponent<NetworkIdentity>())
        {
            Debug.LogError("Mark prefab is missing NetworkIdentity!");
            return;
        }

        NetworkServer.Spawn(mark);

        RpcSetMarkColor(mark, color);

        StartCoroutine(DestroyMarkAfterTime(mark, 2f));
    }

    [ClientRpc]
    void RpcSetMarkColor(GameObject mark, Color color)
    {
        if (mark == null) return;

        Renderer markRenderer = mark.GetComponent<Renderer>();
        if (markRenderer != null)
        {
            markRenderer.material.color = color;
        }
    }

    [Server]
    IEnumerator DestroyMarkAfterTime(GameObject mark, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (mark != null)
        {
            RpcDestroyShadow();
            if (shadow != null)
            {
                Debug.Log("Destroying shadow on SERVER");
                Destroy(shadow);
                shadow = null;
            }
            NetworkServer.Destroy(gameObject);
            NetworkServer.Destroy(mark);
        }
    }

    [ClientRpc]
    void RpcDestroyShadow()
    {
        if (shadow != null)
        {
            Debug.Log("Destroying shadow on client: " + NetworkClient.localPlayer);
            Destroy(shadow);
            shadow = null;
        }
    }

    [Server]
    public void ApplyBump(Vector3 bumpDirection, float hitForce, Vector3 spin)
    {
        bumpOccurred = true;

        float currentTime = Time.time;
        BallState rollbackState = new BallState();
        bool foundValidState = false;

        List<BallState> stateList = new List<BallState>(ballStateBuffer);
        for (int i = stateList.Count - 1; i >= 0; i--)
        {
            if (currentTime - stateList[i].time <= rewindTimeWindow)
            {
                rollbackState = stateList[i];
                foundValidState = true;
                break;
            }
        }

        if (foundValidState)
        {
            rb.position = rollbackState.position;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(bumpDirection * hitForce, ForceMode.Impulse);
        rb.AddTorque(spin, ForceMode.Impulse);
        RpcPlayBumpSound();

        StartCoroutine(SyncBallStateNextFrame());
        StartCoroutine(ResetBumpFlag());
    }

    IEnumerator ResetBumpFlag()
    {
        yield return new WaitForSeconds(0.23f);
        bumpOccurred = false;
    }

    IEnumerator SyncBallStateNextFrame()
    {
        yield return new WaitForFixedUpdate();
        RpcSyncBallState(rb.position, rb.linearVelocity, rb.angularVelocity);
    }

    [ClientRpc]
    void RpcSyncBallState(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
    {
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;
        }

    }

    void OnLastTouchedPlayerChanged(string oldName, string newName)
    {
        Debug.Log($"Ball last touched by: {newName}");
        foreach (PlayerNameTag tag in FindObjectsOfType<PlayerNameTag>())
        {
            tag.UpdateColor(newName);
        }
    }

    [Server]
    public void SetLastTouchedPlayer(string playerName)
    {
        lastPlayerName = playerName;
    }

    [Server]
    public void ServerResetBall()
    {
        Debug.Log($"Server resetting ball {gameObject.name} directly");

        RpcPrepareForDestruction();

        Invoke("DestroyBallOnServer", 2.0f);
    }

    [Server]
    private void DestroyBallOnServer()
    {
        Debug.Log($"Server destroying ball {gameObject.name}");
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcPrepareForDestruction()
    {
        Debug.Log($"Client {(isServer ? "(server)" : "(client)")} preparing for ball destruction");

        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;


        TrajectoryDrawer trajectoryDrawer = GetComponent<TrajectoryDrawer>();
        if (trajectoryDrawer != null)
            trajectoryDrawer.TriggerTrajectoryDisable();


        if (shadow != null)
            Destroy(shadow);
    }


    [Command]
    public void CmdResetBall()
    {
        Debug.LogWarning("CmdResetBall is deprecated. Use ServerResetBall instead.");
        ServerResetBall();
    }


    [ClientRpc]
    void RpcPlayBumpSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        SoundManager.Instance.PlaySound(SoundManager.Instance.bumpSound, audioSource);
    }


    [ClientRpc]
    void RpcPlayGroundSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        SoundManager.Instance.PlaySound(SoundManager.Instance.groundSound, audioSource);
    }
}