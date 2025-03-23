using UnityEngine;
using Mirror;

public class BallController : NetworkBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (transform.parent != null)
        {
            transform.position = transform.parent.position;
        }
    }

    public void AttachToHand(Transform handTransform)
    {
        transform.SetParent(handTransform);
        transform.localPosition = Vector3.zero;
        rb.isKinematic = true;
    }

    public void Serve(Vector3 force)
    {
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);
    }
}
