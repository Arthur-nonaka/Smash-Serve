using UnityEngine;

public class DebugSphere : MonoBehaviour
{
    public Vector3 position;
    public float radius = 1.0f;
    public Color color = Color.red;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(position, radius);
    }
}