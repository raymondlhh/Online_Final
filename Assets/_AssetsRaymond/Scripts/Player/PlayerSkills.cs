using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSkills : MonoBehaviourPunCallbacks
{
    [Header("Random Skills UI")]
    public GameObject[] skillUIs = new GameObject[6]; // Assign 6 skill UI GameObjects in Inspector

    [Header("Centralized Cooldown UI")]
    public Image CooldownBar; // Centralized cooldown bar for all skills
    public TextMeshProUGUI CooldownTime; // Centralized cooldown time text for all skills

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
            // Initialize cooldown to 0 for all skills
            InitializeCooldownUI();
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

    // ===== CENTRALIZED UI MANAGEMENT FUNCTIONS =====

    /// <summary>
    /// Initialize cooldown UI to 0 (skill ready) for all skills
    /// </summary>
    private void InitializeCooldownUI()
    {
        ResetSkillUI();
        if (_photonView != null && _photonView.IsMine)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = 0f;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    /// <summary>
    /// Update the cooldown bar UI for any skill using centralized UI elements
    /// </summary>
    /// <param name="t">Current time value</param>
    /// <param name="max">Maximum time value</param>
    /// <param name="isActivePhase">Whether this is the active skill phase or cooldown phase</param>
    public void UpdateSkillUI(float t, float max, bool isActivePhase)
    {
        if (CooldownBar != null)
        {
            if (isActivePhase)
                CooldownBar.fillAmount = (max - t) / max; // Fills from 0 to 1 during skill duration
            else
                CooldownBar.fillAmount = t / max; // Empties from 1 to 0 during cooldown
        }

        if (CooldownTime != null)
        {
            int seconds = Mathf.CeilToInt(isActivePhase ? t : (max - t));
            CooldownTime.text = seconds.ToString();
        }
    }

    /// <summary>
    /// Update the skill duration bar UI (covering the icon, 0 to 1)
    /// </summary>
    public void UpdateSkillDurationUI(float t, float max)
    {
        if (CooldownBar != null)
            CooldownBar.fillAmount = (max - t) / max; // 0 → 1
        if (CooldownTime != null)
            CooldownTime.text = Mathf.CeilToInt(t).ToString();
    }

    /// <summary>
    /// Update the skill cooldown bar UI (uncovering the icon, 1 to 0)
    /// </summary>
    public void UpdateSkillCooldownUI(float t, float max)
    {
        if (CooldownBar != null)
            CooldownBar.fillAmount = 1f - (t / max); // 1 → 0 as t increases
        if (CooldownTime != null)
            CooldownTime.text = Mathf.CeilToInt(max - t).ToString();
    }

    /// <summary>
    /// Reset the cooldown bar UI to empty (skill ready) using centralized UI elements
    /// </summary>
    public void ResetSkillUI()
    {
        if (CooldownBar != null) CooldownBar.fillAmount = 0f; // Start at 0 (skill ready)
        if (CooldownTime != null) CooldownTime.text = "";
    }

    /// <summary>
    /// Sync the cooldown bar to Photon for network synchronization
    /// </summary>
    public void SyncCooldownBarToPhoton()
    {
        if (_photonView != null && _photonView.IsMine && CooldownBar != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = CooldownBar.fillAmount;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    /// <summary>
    /// Set cooldown percentage directly (for skills that need custom logic)
    /// </summary>
    /// <param name="percent">Cooldown percentage (0-1)</param>
    public void SetCooldownPercent(float percent)
    {
        if (_photonView != null && _photonView.IsMine)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = Mathf.Clamp01(percent);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    /// <summary>
    /// Immediately set skill to used state (for skills activated by left mouse click)
    /// </summary>
    public void SetSkillUsed()
    {
        if (CooldownBar != null)
            CooldownBar.fillAmount = 1f; // Skill is used, bar is full
        
        if (CooldownTime != null)
            CooldownTime.text = "";
            
        // Sync to Photon
        SyncCooldownBarToPhoton();
    }

    /// <summary>
    /// Set the cooldown bar UI to full (1.0) immediately (for transition to cooldown phase)
    /// </summary>
    public void SetSkillBarFull()
    {
        if (CooldownBar != null)
            CooldownBar.fillAmount = 1f;
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
