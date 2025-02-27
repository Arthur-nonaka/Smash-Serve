using UnityEngine;
using UnityEngine.VFX;

public class BallTrailVFX : MonoBehaviour
{
    public VisualEffect vfxGraph;
    public Transform ball;

    void Update()
    {
        if (vfxGraph != null && ball != null)
        {
            Vector3 position = ball.position;
            Vector3 velocity = ball.GetComponent<Rigidbody>().linearVelocity;
            Vector3 invertedVelocity = -velocity;
            float speed = velocity.magnitude;


            vfxGraph.SetVector3("BallPosition", position);
            vfxGraph.SetVector3("BallVelocity", invertedVelocity);
            vfxGraph.SetFloat("BallSpeed", speed);
            vfxGraph.transform.rotation = Quaternion.identity;
        }

    }
}