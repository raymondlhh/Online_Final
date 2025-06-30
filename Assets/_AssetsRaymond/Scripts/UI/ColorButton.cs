using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class ColorButton : MonoBehaviourPunCallbacks
{
    public int colorIndex; // Set in Inspector (0-7)
    public Button button; // Assign the Button component
    public GameObject selectedIndicator; // Assign the TickIcon object

    void Start()
    {
        button.onClick.AddListener(OnClick);
        UpdateButtonState();
    }

    void OnClick()
    {
        if (IsColorAvailable() || IsMyCurrentColor())
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new ExitGames.Client.Photon.Hashtable { { "ColorIndex", colorIndex } }
            );
        }
    }

    bool IsColorAvailable()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("ColorIndex", out object idx) && (int)idx == colorIndex)
                if (player != PhotonNetwork.LocalPlayer)
                    return false;
        }
        return true;
    }

    bool IsMyCurrentColor()
    {
        return PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ColorIndex", out object idx) && (int)idx == colorIndex;
    }

    public void UpdateButtonState()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("ColorIndex", out object idx) && (int)idx == colorIndex)
            {
                selectedIndicator.SetActive(true);
                button.interactable = player == PhotonNetwork.LocalPlayer;
                return;
            }
        }
        selectedIndicator.SetActive(false);
        button.interactable = true;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdateButtonState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateButtonState(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdateButtonState(); }
} 