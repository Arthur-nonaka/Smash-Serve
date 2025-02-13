using UnityEngine;
using System.Collections;

public class VolleyballBall : MonoBehaviour
{
    public float minHitForce = 5f;
    public float maxHitForce = 20f;
    public float gravityScale = 1f;
    public float bounciness = 0.6f;

    private Rigidbody rb;
    private float lastHitTime;
    private bool markCreated = false;

    public GameObject shadowPrefab;
    private GameObject shadow;
    private Transform shadowTransform;

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

        if (shadowPrefab != null)
        {
            shadow = Instantiate(shadowPrefab, transform.position + Vector3.left * 0.1f, Quaternion.Euler(90, 0, 0), transform);
            shadowTransform = shadow.transform;
        }
    }

    void Update()
    {
        UpdateShadow();
    }

    void UpdateShadow()
    {
        if (shadow == null) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
        {
            shadowTransform.position = hit.point + Vector3.up * 0.01f;
        }

        float height = transform.position.y;
        float maxShadowSize = 0.1f; // Maximum size when ball is near ground
        float minShadowSize = 0.0001f; // Minimum size when ball is high
        float maxHeight = 100f; // Maximum considered height

        float scale = Mathf.Lerp(minShadowSize, maxShadowSize, height / maxHeight);
        shadowTransform.localScale = new Vector3(scale, scale, 1);
    }

    public void HitBall(Vector3 direction, float chargeTime, float maxChargeTime)
    {
        float hitPower = Mathf.Lerp(minHitForce, maxHitForce, chargeTime / maxChargeTime);
        rb.linearVelocity = Vector3.zero; // Reset velocity before hitting
        rb.AddForce(direction * hitPower, ForceMode.Impulse);
        lastHitTime = Time.time;
    }

    void AttackBall()
    {
        Vector3 attackDirection = transform.forward + Vector3.up * 0.005f;
        HitBall(attackDirection, maxHitForce, maxHitForce);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !markCreated)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            CreateMark(hitPoint);
            markCreated = true;
            Collider courtCollider = GameObject.FindGameObjectWithTag("Court").GetComponent<Collider>();
            if (courtCollider.bounds.Contains(hitPoint))
            {
                Debug.Log("Ball is in the court");
            }
            else
            {
                Debug.Log("Ball is out of the court");
            }

        }
    }

    void CreateMark(Vector3 position)
    {
        if (markPrefab != null)
        {
            GameObject mark = Instantiate(markPrefab, position, Quaternion.identity);
            StartCoroutine(DestroyMarkAfterTime(mark, 3f));
        }
    }

    private IEnumerator DestroyMarkAfterTime(GameObject mark, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(mark);
        Destroy(gameObject);
    }
}