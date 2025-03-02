using UnityEngine;
using Mirror;

public class Mirror_PlayerSync : NetworkBehaviour
{
    public MonoBehaviour[] localScripts;
    public GameObject[] localObjects;

    Vector3 latestPos;
    Quaternion latestRot;

    void Start()
    {
        if (isLocalPlayer)
        {
        }
        else
        {
            foreach (MonoBehaviour script in localScripts)
            {
                script.enabled = false;
            }
            foreach (GameObject obj in localObjects)
            {
                obj.SetActive(false);
            }
        }
    }

    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        writer.WriteVector3(transform.position);
        writer.WriteQuaternion(transform.rotation);
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        latestPos = reader.ReadVector3();
        latestRot = reader.ReadQuaternion();
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * 5);
            transform.rotation = Quaternion.Lerp(transform.rotation, latestRot, Time.deltaTime * 5);
        }
    }
}
