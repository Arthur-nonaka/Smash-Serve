using System.Collections.Generic;
using UnityEngine;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance;

    private Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadKeybinds();
    }

    public KeyCode GetKey(string action)
    {
        if (keybinds.ContainsKey(action))
        {
            return keybinds[action];
        }

        return KeyCode.None;
    }

    public void SetKey(string action, KeyCode key)
    {
        if (keybinds.ContainsKey(action))
        {
            keybinds[action] = key;
        }
        else
        {
            keybinds.Add(action, key);
        }

        SaveKeybinds();
    }

    private void SaveKeybinds()
    {
        foreach (var keybind in keybinds)
        {
            PlayerPrefs.SetString(keybind.Key, keybind.Value.ToString());
        }

        PlayerPrefs.Save();
    }

    private void LoadKeybinds()
    {
        keybinds.Clear();

        keybinds.Add("Front_Set", KeyCode.F);
        keybinds.Add("Back_Set", KeyCode.R);
        keybinds.Add("Jump", KeyCode.Space);
        keybinds.Add("Dive", KeyCode.Q);
        keybinds.Add("Sprint", KeyCode.LeftShift);
        keybinds.Add("Backwards", KeyCode.LeftControl);

        foreach (var key in keybinds.Keys)
        {
            if (PlayerPrefs.HasKey(key))
            {
                keybinds[key] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString(key));
            }
        }
    }
}