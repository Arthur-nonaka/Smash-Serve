using UnityEngine;
using Mirror;

public class CursorManager : NetworkBehaviour
{
    private void Start()
    {
        if (isLocalPlayer)
        {
            LockCursor(true);
        }
    }

    private void OnDisable()
    {
        if (isLocalPlayer)
        {
            FreeCursor();
        }
    }

    private void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void FreeCursor()
    {
        LockCursor(false);
    }
}
