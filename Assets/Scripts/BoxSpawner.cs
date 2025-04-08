using UnityEngine;
using System.Collections;

public class BoxSpawner : MonoBehaviour
{
    public GameObject greenBoxPrefab;
    private GameObject currentBox;


    void Start()
    {
    }

    public void SpawnBox(Vector3 spawnAreaMin, Vector3 spawnAreaMax)
    {
        StartCoroutine(SpawnBoxWithDelay(spawnAreaMin, spawnAreaMax, 0.2f));
    }

    private IEnumerator SpawnBoxWithDelay(Vector3 spawnAreaMin, Vector3 spawnAreaMax, float delay)
    {
        if (currentBox != null)
        {
            Destroy(currentBox);
        }

        yield return new WaitForSeconds(delay);

        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        float z = Random.Range(spawnAreaMin.z, spawnAreaMax.z);

        Vector3 spawnPosition = new Vector3(x, y, z);
        currentBox = Instantiate(greenBoxPrefab, spawnPosition, Quaternion.identity);
    }
}