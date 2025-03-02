using UnityEngine;
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
        Vector3 magnusForce = magnusCoefficient * airDensity * Mathf.PI * Mathf.Pow(radius, 3) * Vector3.Cross(angularVelocity, velocity);
        rb.AddForce(magnusForce);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        if (collision.gameObject.CompareTag("Ground") && !markCreated)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Collider courtCollider = GameObject.FindGameObjectWithTag("Court").GetComponent<Collider>();
            bool isIn = courtCollider.bounds.Contains(hitPoint);

            Color markColor = isIn ? Color.green : Color.red;

            CreateMark(hitPoint, markColor);

            markCreated = true;
            GetComponent<Collider>().enabled = false;
        }
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
        float currentTime = Time.time;
        BallState rollbackState = new BallState();
        bool foundValidState = false;

        foreach (BallState state in ballStateBuffer)
        {
            if (currentTime - state.time <= rewindTimeWindow)
            {
                rollbackState = state;
                foundValidState = true;
            }
            else
            {
                break;
            }
        }

        if (foundValidState)
        {
            rb.position = rollbackState.position;
            rb.linearVelocity = rollbackState.velocity;
            rb.angularVelocity = rollbackState.angularVelocity;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(bumpDirection * hitForce, ForceMode.Impulse);
        rb.AddTorque(spin, ForceMode.Impulse);

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
}
