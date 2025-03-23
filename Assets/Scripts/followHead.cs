using UnityEngine;

public class CameraFollowHead : MonoBehaviour
{
    public Transform head; // Arraste o osso da cabeça aqui no Inspector
    public Vector3 offset; // Ajuste um pequeno deslocamento se necessário

    void Update()
    {
        if (head != null)
        {
            transform.position = head.position + offset + head.forward * 0.2f;
        }
    }
}