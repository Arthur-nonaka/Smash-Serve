using Mirror;
using UnityEngine;

public class PlayerChat : NetworkBehaviour
{
    [Command]
    public void CmdSendChatMessage(string message)
    {
        PlayerController controller = GetComponent<PlayerController>();
        string fullMessage = $"[{controller.GetPlayerName()}] {message}";
        ChatManager.Instance.RpcReceiveChatMessage(fullMessage);
    }
}
