using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

public class DiveHitbox : MonoBehaviourPun
{

    public float diveHitForce = 3.0f;
    public Transform playerCamera;
    private GameObject ball;

    private Collider hitboxCollider;
    private Collider ownershipHitboxCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ownershipHitboxCollider = transform.Find("Hitbox-Bump-Ownership").GetComponent<Collider>();
        hitboxCollider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            ball = other.gameObject;
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            RequestOwnership(ball);
            if (ballRb != null)
            {

                float hitPower = diveHitForce;

                float randomDirection = Random.Range(-0.03f, 0.03f);
                Vector3 spin = playerCamera.transform.right * 0.08f + playerCamera.transform.forward * randomDirection;

                StartCoroutine(WaitForOwnershipAndBump(ballRb, hitPower, spin));
            }
        }
    }

    IEnumerator WaitForOwnershipAndBump(Rigidbody ballRb, float hitPower, Vector3 spin)
    {

        while (!ball.GetComponent<PhotonView>().IsMine)
        {
            yield return null;
        }

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.AddForce(Vector3.up * hitPower, ForceMode.Impulse);
        ballRb.AddTorque(spin, ForceMode.Impulse);

        // photonView.RPC("SyncBallState", RpcTarget.Others, ball.transform.position, ballRb.linearVelocity, ballRb.angularVelocity);
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
