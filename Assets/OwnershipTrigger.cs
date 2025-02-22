using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class OwnershipTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            GameObject ball = other.gameObject;
            Debug.Log("OwnershipTrigger: Ball entered. Requesting ownership.");
            RequestOwnership(ball);
        }
    }

    void RequestOwnership(GameObject obj)
    {
        PhotonView photonView = obj.GetComponent<PhotonView>();
        if (photonView != null && !photonView.IsMine)
        {
            Debug.Log($"Requesting ownership of {obj.name}");
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
    }
}