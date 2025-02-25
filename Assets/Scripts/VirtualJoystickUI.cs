using UnityEngine;
using UnityEngine.UI;

public class VirtualJoystickUI : MonoBehaviour 
{
    [Header("UI References")]
    public RectTransform joystickBG;     
    public RectTransform joystickHandle;   

    [Header("Joystick Settings")]
    public float joystickMaxOffset = 50f;

    public float virtualJoystickOffsetX = 0f;

    void Update()
    {
        float normalizedHorizontal = virtualJoystickOffsetX / joystickMaxOffset;
        normalizedHorizontal = Mathf.Clamp(normalizedHorizontal, -1f, 1f);

        joystickHandle.anchoredPosition = new Vector2(normalizedHorizontal * joystickMaxOffset, 0f);
    }
}