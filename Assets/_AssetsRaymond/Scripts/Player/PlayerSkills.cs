using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class PlayerSkills : MonoBehaviourPunCallbacks
{
    [Header("Random Skills UI")]
    public GameObject[] skillUIs = new GameObject[6]; // Assign 6 skill UI GameObjects in Inspector

    private PhotonView _photonView;

    // --- RPC Relay Section ---
    // Add references to your skill scripts here (assign in inspector or via GetComponentInChildren)
    
    public BlackHoleSkill blackHoleSkill;
    public FreezeGunSkill freezeGunSkill;
    public InvisibilitySkill invisibilitySkill;
    public SlowGunSkill slowGunSkill;
    public SprintBoostSkill sprintBoostSkill;
    public TeleportSkill teleportSkill;
    
    
    // Add other skill scripts as needed

    private void Awake()
    {
        // Auto-assign if not set in inspector
        if (slowGunSkill == null) slowGunSkill = GetComponentInChildren<SlowGunSkill>(true);
        if (blackHoleSkill == null) blackHoleSkill = GetComponentInChildren<BlackHoleSkill>(true);
        if (freezeGunSkill == null) freezeGunSkill = GetComponentInChildren<FreezeGunSkill>(true);
        if (teleportSkill == null) teleportSkill = GetComponentInChildren<TeleportSkill>(true);
        if (invisibilitySkill == null) invisibilitySkill = GetComponentInChildren<InvisibilitySkill>(true);
        if (sprintBoostSkill == null) sprintBoostSkill = GetComponentInChildren<SprintBoostSkill>(true);
        // Add other skills as needed
    }

    private void Start()
    {
        _photonView = GetComponentInParent<PhotonView>();

        // We only want to control the UI for the local player.
        if (_photonView != null && _photonView.IsMine)
        {
            UpdateSkillsUI();
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

    [PunRPC]
    public void ShowTPSlowGun(bool show)
    {
        if (slowGunSkill != null)
            slowGunSkill.ShowTPSlowGun(show);
    }

    [PunRPC]
    public void ShowTPBlackHole(bool show)
    {
        if (blackHoleSkill != null)
            blackHoleSkill.ShowTPBlackHole(show);
    }

    [PunRPC]
    public void ShowTPFreezeGun(bool show)
    {
        if (freezeGunSkill != null)
            freezeGunSkill.ShowTPFreezeGun(show);
    }

    [PunRPC]
    public void ShowTPSprintBoost(bool show)
    {
        if (sprintBoostSkill != null)
            sprintBoostSkill.gameObject.SetActive(show);
    }

    [PunRPC]
    public void ShowTPInvisibility(bool show)
    {
        if (invisibilitySkill != null)
            invisibilitySkill.gameObject.SetActive(show);
    }

    [PunRPC]
    public void ShowTPTeleport(bool show)
    {
        if (teleportSkill != null)
            teleportSkill.gameObject.SetActive(show);
    }

    // Add more relay methods for other skills as needed

    // Update is called once per frame
    void Update()
    {
        
    }
}
