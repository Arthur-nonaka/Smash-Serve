// using UnityEngine;
// using Mirror;

// public class OwnershipTrigger : NetworkBehaviour
// {
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Ball"))
//         {
//             GameObject ball = other.gameObject;
//             Debug.Log("OwnershipTrigger: Ball entered. Requesting ownership.");
//             RequestOwnership(ball);
//         }
//     }

//     void RequestOwnership(GameObject obj)
//     {
//         NetworkIdentity identity = obj.GetComponent<NetworkIdentity>();
//         if (identity != null && !identity.hasAuthority)
//         {
//             Debug.Log("Not owner");
//             Debug.Log($"Requesting ownership of {obj.name}");
//             CmdRequestOwnership(identity.netId);
//         }
//     }

//     [Command]
//     void CmdRequestOwnership(uint netId)
//     {
//         if (NetworkIdentity.spawned.TryGetValue(netId, out NetworkIdentity identity))
//         {
//             // Remove previous authority if necessary, then assign to the requesting client.
//             if (identity.clientAuthorityOwner != connectionToClient)
//             {
//                 if (identity.clientAuthorityOwner != null)
//                 {
//                     identity.RemoveClientAuthority();
//                 }
//                 identity.AssignClientAuthority(connectionToClient);
//             }
//         }
//     }
// }
