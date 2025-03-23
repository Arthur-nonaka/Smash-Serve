using UnityEngine;

public class BlockHitbox : MonoBehaviour
{
    private PlayerController playerMovement;

    void Start()
    {
        playerMovement = GetComponentInParent<PlayerController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            playerMovement.CmdNotifyBallTouched(true);
            Debug.Log("Ball touched the block!");
        }
    }

}
