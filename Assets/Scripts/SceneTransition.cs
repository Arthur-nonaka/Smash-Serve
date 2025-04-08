using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string tutorialSceneName;

    public void LoadTutorialScene()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }
}