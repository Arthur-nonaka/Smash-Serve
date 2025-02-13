using UnityEngine;

public class VolleyballBall : MonoBehaviour
{
    public float minHitForce = 5f;
    public float maxHitForce = 20f;
    public float gravityScale = 1f;
    public float bounciness = 0.6f;

    private Rigidbody rb;
    private float lastHitTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Add bouncy physics material
        PhysicsMaterial bounceMaterial = new PhysicsMaterial();
        bounceMaterial.bounciness = bounciness;
        bounceMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        bounceMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        GetComponent<SphereCollider>().material = bounceMaterial;
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
}
