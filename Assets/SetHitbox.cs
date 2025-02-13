using UnityEngine;

public class SetHitbox : MonoBehaviour
{
    private PlayerController playerController;

    void Start()
    {
        // Procura o PlayerController no objeto pai (o jogador)
        playerController = GetComponentInParent<PlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            playerController.SetBall(other.gameObject);
            Debug.Log("Bola dentro da hitbox de set!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            playerController.ClearBall();
            Debug.Log("Bola saiu da hitbox de set!");
        }
    }
}