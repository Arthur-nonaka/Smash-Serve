using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance;

    [Header("UI References")]
    public Transform chatContent;
    public GameObject chatMessagePrefab;
    public ScrollRect scrollRect;
    public InputField chatInput;

    private bool chatActive = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Start with chat input hidden.
    }

    void Update()
    {

        if (chatActive && Input.GetKeyDown(KeyCode.Return))
        {
            if (!string.IsNullOrEmpty(chatInput.text))
                SendMessageFromPlayer(chatInput.text);

            SetChatActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
            SetChatActive(!chatActive);

    }

    private void SetChatActive(bool active)
    {
        chatActive = active;
        chatInput.gameObject.SetActive(active);

        if (active)
        {
            chatInput.ActivateInputField();
            chatInput.text = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        EventSystem.current.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    public void SpawnChatMessage(string message)
    {
        if (chatMessagePrefab != null && chatContent != null)
        {
            GameObject messageObj = Instantiate(chatMessagePrefab, chatContent);
            messageObj.transform.SetAsLastSibling();

            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());

            messageObj.GetComponent<Text>().text = message;

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        else
        {
            Debug.LogWarning("chatMessagePrefab or chatContent is not assigned.");
        }
    }
    public void SendMessageFromPlayer(string message)
    {
        // Ensure we have a connection and a local player.
        if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            PlayerChat playerChat = NetworkClient.connection.identity.GetComponent<PlayerChat>();
            if (playerChat != null)
            {
                playerChat.CmdSendChatMessage(message);
            }
            else
            {
                Debug.LogWarning("PlayerChat component not found on the local player.");
            }
        }
        else
        {
            Debug.LogWarning("No local player found to send the chat message.");
        }
    }

    [ClientRpc]
    public void RpcReceiveChatMessage(string message)
    {
        SpawnChatMessage(message);
    }
}
