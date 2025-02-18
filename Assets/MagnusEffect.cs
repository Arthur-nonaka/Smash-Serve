using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MagnusEffect : MonoBehaviour
{
    public float radius = 0.2f;
    public float airDensity = 1.2f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 angularVelocity = rb.angularVelocity;

        Vector3 magnusForce = airDensity * Mathf.PI * Mathf.Pow(radius, 3) * Vector3.Cross(angularVelocity, velocity);

        rb.AddForce(magnusForce);
    }
}