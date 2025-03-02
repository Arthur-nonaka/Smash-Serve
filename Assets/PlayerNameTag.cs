using UnityEngine;
using TMPro;
using Mirror;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";

    private void Start()
    {
        if (isLocalPlayer)
        {
            string localName = PlayerPrefs.GetString("PlayerName", "Player");
            CmdSetName(localName);
        }

        if (isLocalPlayer && nameText != null)
        {
            nameText.gameObject.SetActive(false);
        }
    }

    [Command]
    private void CmdSetName(string newName)
    {
        playerName = newName;
    }

    private void OnNameChanged(string oldName, string newName)
    {
        nameText.text = newName;
    }
}
