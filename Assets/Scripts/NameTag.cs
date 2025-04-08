using UnityEngine;
using Mirror;

public class NameTag : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        FindLocalCamera();
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
        }
    }

    private void FindLocalCamera()
    {
        if (NetworkClient.localPlayer != null)
        {
            cam = NetworkClient.localPlayer.GetComponentInChildren<Camera>();
        }

        if (cam == null)
        {
            Debug.LogWarning("Local player's camera not found.");
        }
    }
}
