using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class PlayerSkills : MonoBehaviourPunCallbacks
{
    [Header("Random Skills UI")]
    public GameObject[] skillUIs = new GameObject[6]; // Assign 6 skill UI GameObjects in Inspector

    [Header("Timer Display")]
    public TextMeshProUGUI timerText;

    private PhotonView _photonView;

    private void Start()
    {
        _photonView = GetComponentInParent<PhotonView>();

        // We only want to control the UI for the local player.
        if (_photonView != null && _photonView.IsMine)
        {
            UpdateSkillsUI();
            if (GameManager.Instance != null && timerText != null)
            {
                GameManager.Instance.RegisterTimerText(timerText);
            }
        }
        else
        {
            // For remote players, or if there's no PhotonView, disable all skill UIs.
            // This is especially important as this script is on a component in the FP_View.
            foreach (var skillUI in skillUIs)
            {
                if (skillUI != null) skillUI.SetActive(false);
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Only update if the view is ours and properties are relevant
        if (_photonView != null && _photonView.IsMine && targetPlayer == _photonView.Owner)
        {
            if (changedProps.ContainsKey("SkillIndex"))
            {
                UpdateSkillsUI();
            }
        }
    }

    private void UpdateSkillsUI()
    {
        if (_photonView == null || !_photonView.IsMine || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        Player owner = _photonView.Owner;
        if (owner == null)
        {
            return;
        }

        // Get the player's assigned skill index
        if (owner.CustomProperties.TryGetValue("SkillIndex", out object skillIndexObj))
        {
            int skillIndex = (int)skillIndexObj;
            
            // Disable all skill UIs first
            foreach (var skillUI in skillUIs)
            {
                if (skillUI != null) skillUI.SetActive(false);
            }
            
            // Enable the correct skill UI
            if (skillIndex >= 0 && skillIndex < skillUIs.Length && skillUIs[skillIndex] != null)
            {
                skillUIs[skillIndex].SetActive(true);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
