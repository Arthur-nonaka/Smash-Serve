using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

public class VolleyballBall : MonoBehaviourPun, IPunObservable
{
    public float minHitForce = 5f;
    public float maxHitForce = 20f;
    public float gravityScale = 1f;
    public float bounciness = 0.6f;

    public float radius = 0.2f;
    public float airDensity = 1.2f;
    public float magnusCoefficient = 0.1f;

    private Rigidbody rb;
    private float lastHitTime;
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
            shadow.transform.SetParent(transform);
            shadow.transform.localPosition = new Vector3(0, 0, 0);

            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.sortingOrder = -1;
            shadowRenderer.color = Color.black;
            Color currentColor = shadowRenderer.color;
            shadowRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.7f);

            shadow.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
    }

    void Update()
    {
        UpdateShadow();
    }

    void FixedUpdate()
    {
        ApplyMagnusEffect();
    }

    void UpdateShadow()
    {
        if (shadow == null) return;

        int layerMask = ~(
            LayerMask.GetMask("Ball") |
            LayerMask.GetMask("Player") |
            LayerMask.GetMask("Hitbox") |
            LayerMask.GetMask("Net")
        );

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            shadow.transform.position = hit.point + Vector3.up * 0.05f;
        }

        float height = transform.position.y;
        float maxShadowSize = 0.4f;
        float minShadowSize = 0.0001f;
        float maxHeight = 200f;

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
        if (collision.gameObject.CompareTag("Ground") && !markCreated)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Collider courtCollider = GameObject.FindGameObjectWithTag("Court").GetComponent<Collider>();
            if (courtCollider.bounds.Contains(hitPoint))
            {
                CreateMark(hitPoint, Color.green);
                Debug.Log("Ball is in the court");

            }
            else
            {
                CreateMark(hitPoint, Color.red);
                Debug.Log("Ball is out of the court");
            }
            markCreated = true;

        }
    }

    void CreateMark(Vector3 position, Color color)
    {
        if (markPrefab != null)
        {
            GameObject mark = Instantiate(markPrefab, position, Quaternion.identity);
            Renderer markRenderer = mark.GetComponent<Renderer>();
            if (markRenderer != null)
            {
                markRenderer.material.color = color;
            }
            StartCoroutine(DestroyMarkAfterTime(mark, 3f));
        }
    }

    private IEnumerator DestroyMarkAfterTime(GameObject mark, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(mark);
        Destroy(gameObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send data to other players
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext(rb.angularVelocity);
        }
        else
        {
            // Receive data from other players
            rb.position = (Vector3)stream.ReceiveNext();
            rb.rotation = (Quaternion)stream.ReceiveNext();
            rb.linearVelocity = (Vector3)stream.ReceiveNext();
            rb.angularVelocity = (Vector3)stream.ReceiveNext();
        }
    }
}