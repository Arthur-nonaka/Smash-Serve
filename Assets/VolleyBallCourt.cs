using UnityEngine;

public class VolleyballCourt : MonoBehaviour
{
    public GameObject courtPrefab;
    public GameObject netPrefab;

    void Start()
    {
        SpawnCourt();
    }

    void SpawnCourt()
    {
        GameObject court = Instantiate(courtPrefab, Vector3.zero, Quaternion.identity);
        court.name = "Court";
        
        GameObject net = Instantiate(netPrefab, new Vector3(0, 1.2f, 0), Quaternion.identity);
        net.name = "Net";
        
        BoxCollider netCollider = net.AddComponent<BoxCollider>();
        netCollider.size = new Vector3(0.1f, 2.4f, 9f);
        netCollider.isTrigger = true;
    }
}
