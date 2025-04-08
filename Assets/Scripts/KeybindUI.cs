using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class KeybindUI : MonoBehaviour
{
    public string actionName;
    public TMP_Text keyText;

    private bool isRebinding = false;

    private void Start()
    {
        UpdateKeyText();
    }

    public void StartRebinding()
    {
        if (isRebinding) return;

        isRebinding = true;
        keyText.text = "Press a key...";
        Debug.Log($"Starting rebinding for {actionName}");
        StartCoroutine(WaitForKeyPress());
    }

    private IEnumerator WaitForKeyPress()
    {
        while (!Input.anyKeyDown)
        {
            yield return null;
        }

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                KeybindManager.Instance.SetKey(actionName, key);
                break;
            }
        }

        isRebinding = false;
        UpdateKeyText();
    }

    private void UpdateKeyText()
    {
        keyText.text = KeybindManager.Instance.GetKey(actionName).ToString();
    }
}