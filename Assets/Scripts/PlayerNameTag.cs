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

    void OnNameChanged(string oldName, string newName)
    {
        if (nameText != null)
            nameText.text = newName;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    [Command]
    private void CmdSetName(string newName)
    {
        playerName = newName;
    }

    public void SetName(string newName)
    {
        playerName = newName;
        if (nameText != null)
            nameText.text = newName;
    }

    public void SetColor(Color color)
    {
        if (nameText != null)
            nameText.color = color;
    }

    public void UpdateColor(string lastTouchedPlayer)
    {
        Debug.Log($"PlayerNameTag '{playerName}' comparing with lastTouchedPlayer '{lastTouchedPlayer}'");

        if (playerName.Equals(lastTouchedPlayer))
        {
            // SetColor(Color.red);
        }
        else
        {
            // SetColor(Color.white);
        }
    }
}
