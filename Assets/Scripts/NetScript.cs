using UnityEngine;

public class NetScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Check if the collision point is near the top of the net
            Vector3 hitPoint = collision.contacts[0].point;
            if (IsNearTopOfNet(hitPoint))
            {
                Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
                if (ballRb != null)
                {
                    // Calculate a realistic deflection:
                    // Reduce forward speed, add upward bounce.
                    Vector3 currentVelocity = ballRb.linearVelocity;
                    Vector3 deflection = new Vector3(currentVelocity.x * 0.5f, Mathf.Abs(currentVelocity.y) + 2f, currentVelocity.z * 0.5f);

                    ballRb.linearVelocity = deflection;

                    Debug.Log("Ball hit the net top and deflected realistically.");
                }
            }
        }
    }

    bool IsNearTopOfNet(Vector3 hitPoint)
    {
        // Define a threshold for what is considered "near the top" of the net.
        // For example, if the net's top is at a known Y value, you could:
        float netTopY = transform.position.y + (transform.localScale.y / 2f);
        return Mathf.Abs(hitPoint.y - netTopY) < 0.2f; // Adjust threshold as needed
    }
}
