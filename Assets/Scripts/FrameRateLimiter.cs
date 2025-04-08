using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    public int targetFrameRate = 240; 

    void Start()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0; 
    }
}