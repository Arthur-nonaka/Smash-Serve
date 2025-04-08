using UnityEngine;
using Mirror;
public class NetScript : NetworkBehaviour
{

    void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Ball"))
        {
            Debug.Log("Ball hit the net.");
            Vector3 hitPoint = collision.ClosestPoint(transform.position);
            if (IsNearTopOfNet(hitPoint))
            {
                if (isServer)
                {
                    Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
                    if (ballRb != null)
                    {
                        Vector3 currentVelocity = ballRb.linearVelocity;
                        Vector3 deflection = new Vector3(currentVelocity.x * Random.Range(0.1f, 0.3f), 1f, currentVelocity.z * Random.Range(0.1f, 0.3f));

                        ballRb.linearVelocity = deflection;

                        Vector3 randomSpin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 2f;

                        ballRb.angularVelocity = randomSpin;

                        NetworkIdentity ballIdentity = collision.gameObject.GetComponent<NetworkIdentity>();
                        if (!ballIdentity != null)
                        {
                            RpcUpdateBallVelocity(ballIdentity.netId, deflection, randomSpin);
                        }

                        Debug.Log("Ball hit the net top and deflected realistically.");
                    }
                }
            }
            else
            {
                if (isServer)
                {
                    Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
                    if (ballRb != null)
                    {
                        Vector3 currentVelocity = ballRb.linearVelocity;
                        Vector3 deflection = new Vector3(currentVelocity.x * 0.1f, 0.3f, currentVelocity.z * 0.1f);

                        ballRb.linearVelocity = deflection;

                        Vector3 randomSpin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 2f;

                        ballRb.angularVelocity = randomSpin;

                        NetworkIdentity ballIdentity = collision.gameObject.GetComponent<NetworkIdentity>();
                        if (!ballIdentity != null)
                        {
                            RpcUpdateBallVelocity(ballIdentity.netId, deflection, randomSpin);
                        }

                        Debug.Log("Ball hit the net and deflected realistically.");
                    }
                }
            }
        }
    }

    [ClientRpc]
    void RpcUpdateBallVelocity(uint ballNetId, Vector3 newVelocity, Vector3 newAngularVelocity)
    {
        if (NetworkServer.spawned.TryGetValue(ballNetId, out NetworkIdentity ballIdentity))
        {
            Rigidbody ballRb = ballIdentity.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.linearVelocity = newVelocity;
                ballRb.angularVelocity = newAngularVelocity;
            }
        }
    }

    bool IsNearTopOfNet(Vector3 hitPoint)
    {
        float netTopY = transform.position.y + (transform.localScale.y / 2f);
        Debug.Log($"Net top Y: {netTopY}, Hit point Y: {hitPoint.y}");
        return Mathf.Abs(hitPoint.y - netTopY) < 64.566f;
    }
}
