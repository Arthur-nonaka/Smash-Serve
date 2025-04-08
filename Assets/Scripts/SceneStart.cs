using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections;

public class SceneStart : MonoBehaviour
{

    public NetworkManager networkManager;
    private UIManager uiManager;

    void Start()
    {
        StartCoroutine(StartDelay(0.5f)); 
    }

    private IEnumerator StartDelay(float delay)
    {
        yield return new WaitForSeconds(delay); 
        uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found in the scene. Please ensure it is present.");
        }
        Debug.Log("SceneStart script started. UIManager found: " + (uiManager != null));

        networkManager.StartHost();
        uiManager.ShowGameplayPanel();
    }

    void Update()
    {
        
    }
}
