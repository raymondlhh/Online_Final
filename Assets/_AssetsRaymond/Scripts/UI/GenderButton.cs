using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GenderButton : MonoBehaviourPunCallbacks
{
    public int genderIndex; // 0 = Male, 1 = Female
    public Button button;
    public GameObject selectedIndicator; // MaleSelected or FemaleSelected

    void Start()
    {
        button.onClick.AddListener(OnClick);
        UpdateButtonState();
    }

    void OnClick()
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new ExitGames.Client.Photon.Hashtable { { "GenderIndex", genderIndex } }
        );
    }

    public void UpdateButtonState()
    {
        bool isMine = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("GenderIndex", out object idx) && (int)idx == genderIndex;
        selectedIndicator.SetActive(isMine);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("GenderIndex"))
            UpdateButtonState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateButtonState(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdateButtonState(); }
} 