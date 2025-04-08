using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindRebinder : MonoBehaviour
{
    public InputActionReference actionToRebind;
    public TMPro.TextMeshProUGUI bindingDisplayText;

    private void Start()
    {
        LoadKeybinds();
        UpdateBindingDisplay();
    }

    public void StartRebinding()
    {
        actionToRebind.action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnComplete(operation =>
            {
                Debug.Log($"Rebound to: {operation.selectedControl}");
                operation.Dispose();
                UpdateBindingDisplay();
                SaveKeybinds();
            })
            .Start();
    }

    private void UpdateBindingDisplay()
    {
        if (bindingDisplayText != null)
        {
            bindingDisplayText.text = actionToRebind.action.bindings[0].effectivePath;
        }
    }

    private void SaveKeybinds()
    {
        string bindingOverride = actionToRebind.action.bindings[0].overridePath;
        PlayerPrefs.SetString(actionToRebind.action.name, bindingOverride);
        PlayerPrefs.Save();
        Debug.Log($"Keybind for {actionToRebind.action.name} saved: {bindingOverride}");
    }

    private void LoadKeybinds()
    {
        if (PlayerPrefs.HasKey(actionToRebind.action.name))
        {
            string savedBinding = PlayerPrefs.GetString(actionToRebind.action.name);
            actionToRebind.action.ApplyBindingOverride(0, savedBinding);
            Debug.Log($"Loaded keybind for {actionToRebind.action.name}: {savedBinding}");
        }
    }
}