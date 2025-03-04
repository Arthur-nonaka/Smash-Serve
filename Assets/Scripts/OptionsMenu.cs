using UnityEngine;

public class OptionsMenu : MonoBehaviour
{

    private UIManager UIManager;

    private bool isPaused = false;

    void Start()
    {
        UIManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (!isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                UIManager.ShowOptionsPanel();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                UIManager.HideOptionsPanel();
            }

            isPaused = !isPaused;
        }
    }
}
