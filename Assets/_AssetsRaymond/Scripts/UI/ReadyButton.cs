using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class ReadyButton : MonoBehaviourPunCallbacks
{
    public Button readyButton;
    public TMP_Text readyButtonText;
    public GameObject readySelected; // The UI object to show/hide
    public ChooseCharacterManager chooseCharacterManager;

    void Start()
    {
        readyButton.onClick.AddListener(OnClick);
        UpdateReadyUI();
    }

    void OnClick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Only allow host to start if all others are ready
            if (chooseCharacterManager.AllClientsReady())
            {
                chooseCharacterManager.OnHostStartGame();
            }
        }
        else
        {
            bool isReady = IsLocalPlayerReady();
            SetLocalPlayerReady(!isReady);
        }
    }

    bool IsLocalPlayerReady()
    {
        return PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsReady", out object val) && (bool)val;
    }

    void SetLocalPlayerReady(bool ready)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new ExitGames.Client.Photon.Hashtable { { "IsReady", ready } }
        );
    }

    public void UpdateReadyUI()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            readyButtonText.text = "Start";
            readySelected.SetActive(false);
            readyButton.interactable = chooseCharacterManager.AllClientsReady();
        }
        else
        {
            readyButtonText.text = "Ready";
            bool isReady = IsLocalPlayerReady();
            readySelected.SetActive(isReady);
            readyButton.interactable = true;
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdateReadyUI();
        chooseCharacterManager.UpdateAllReadyUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateReadyUI();
        chooseCharacterManager.UpdateAllReadyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateReadyUI();
        chooseCharacterManager.UpdateAllReadyUI();
    }
} 