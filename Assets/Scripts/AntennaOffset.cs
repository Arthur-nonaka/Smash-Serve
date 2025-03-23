using UnityEngine;

public class AntennaOffset : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            VolleyballBall ball = other.GetComponent<VolleyballBall>();
            if (ball != null)
            {
                TeamManager.Instance.BallPassedOffAntenna(ball);
            }
        }
    }
}
