using UnityEngine;

public class PlayerSeparation : MonoBehaviour
{
    public float separationRadius = 1.5f;
    public float separationForce = 5f;

    void FixedUpdate()
    {
        Collider[] nearbyPlayers = Physics.OverlapSphere(transform.position, separationRadius, LayerMask.GetMask("PlayerLayer"));

        foreach (Collider player in nearbyPlayers)
        {
            if (player.gameObject != gameObject)
            {
                Vector3 direction = transform.position - player.transform.position;
                direction.y = 0;
                transform.position += direction.normalized * separationForce * Time.fixedDeltaTime;
            }
        }
    }
}