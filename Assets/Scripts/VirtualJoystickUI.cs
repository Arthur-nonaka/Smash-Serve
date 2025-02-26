using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VirtualJoystickUI : MonoBehaviour
{
    [Header("UI References")]
    private RectTransform joystickBG;
    private RectTransform joystickHandle;

    [Header("Joystick Settings")]
    public float joystickMaxOffset = 50f;

    public float virtualJoystickOffsetX = 0f;

    void Start()
    {
        joystickBG = GameObject.FindGameObjectWithTag("JoystickBG").GetComponent<RectTransform>();
        joystickHandle = GameObject.FindGameObjectWithTag("JoystickHandle").GetComponent<RectTransform>();

    }

    void Update()
    {
        float normalizedHorizontal = virtualJoystickOffsetX / joystickMaxOffset;
        normalizedHorizontal = Mathf.Clamp(normalizedHorizontal, -1f, 1f);

        joystickHandle.anchoredPosition = new Vector2(normalizedHorizontal * joystickMaxOffset, 0f);
    }
}