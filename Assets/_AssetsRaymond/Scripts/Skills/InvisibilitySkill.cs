using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class InvisibilitySkill : MonoBehaviourPunCallbacks
{
    public PlayerVisibility playerVisibility; // Assign in inspector or via GetComponentInParent
    public GameObject tpViewObject; // Assign in inspector or via GetComponentInParent
    public GameObject tpPlayerUI;   // Assign in inspector or via GetComponentInParent
    public GameObject invisibilityPanel; // Assign in inspector or via search
    public float duration = 10f;
    private int cloakedPlayerLayer;
    private bool isInvisible = false;
    private Coroutine invisCoroutine;
    private bool isOnCooldown = false;
    private float timer = 0f;

    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 10f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

    // Reference to PlayerSkills for centralized UI management
    private PlayerSkills playerSkills;

    // Start is called before the first frame update
    void Start()
    {
        if (playerVisibility == null) playerVisibility = GetComponentInParent<PlayerVisibility>();
        if (tpViewObject == null && playerVisibility != null) tpViewObject = playerVisibility.tpViewObject;
        if (tpPlayerUI == null && playerVisibility != null) tpPlayerUI = playerVisibility.tpPlayerUI;
        cloakedPlayerLayer = LayerMask.NameToLayer("CloakedPlayer");
        if (invisibilityPanel == null)
        {
            var fpUI = transform.root.Find("FP_PlayerUI/PlayerPanels/InvisibilityPanel");
            if (fpUI != null) invisibilityPanel = fpUI.gameObject;
        }
        
        // Get reference to PlayerSkills
        playerSkills = GetComponentInParent<PlayerSkills>();
        if (playerSkills == null)
            playerSkills = FindObjectOfType<PlayerSkills>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;
        if (Input.GetKeyDown(KeyCode.E) && !isInvisible && !isOnCooldown)
        {
            isInvisible = true;
            isOnCooldown = false;
            if (playerVisibility != null)
            {
                playerVisibility.photonView.RPC("SetInvisibilityRelay", Photon.Pun.RpcTarget.All);
                playerVisibility.photonView.RPC("SetPlayerLayerCloaked", Photon.Pun.RpcTarget.All);
            }
            if (invisCoroutine != null) StopCoroutine(invisCoroutine);
            invisCoroutine = StartCoroutine(InvisibilityDurationAndCooldown());
        }
    }

    public void SetInvisibility()
    {
        // Hide TP_View and TP_PlayerUI
        if (playerVisibility != null)
        {
            playerVisibility.SetThirdPersonVisibility(false);
        }
        else
        {
            if (tpViewObject != null) tpViewObject.SetActive(false);
            if (tpPlayerUI != null) tpPlayerUI.SetActive(false);
        }
        // (Layer change now handled by PlayerVisibility)
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void UpdateUI(float t, float max, bool isActivePhase)
    {
        if (playerSkills != null)
        {
            playerSkills.UpdateSkillUI(t, max, isActivePhase);
            playerSkills.SyncCooldownBarToPhoton();
        }
    }

    private IEnumerator InvisibilityDurationAndCooldown()
    {
        // Skill active phase
        timer = activeDuration;
        if (photonView.IsMine && invisibilityPanel != null)
            invisibilityPanel.SetActive(true);
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (playerSkills != null) playerSkills.UpdateSkillDurationUI(timer, activeDuration);
            playerSkills?.SyncCooldownBarToPhoton();
            yield return null;
        }
        if (photonView.IsMine && invisibilityPanel != null)
            invisibilityPanel.SetActive(false);
        isInvisible = false;
        if (playerVisibility != null)
        {
            playerVisibility.photonView.RPC("UnsetInvisibilityRelay", Photon.Pun.RpcTarget.All);
            playerVisibility.photonView.RPC("SetPlayerLayerNormal", Photon.Pun.RpcTarget.All);
        }
        // Set bar to full at the start of cooldown
        if (playerSkills != null) playerSkills.SetSkillBarFull();
        if (playerSkills != null) playerSkills.UpdateSkillCooldownUI(0, cooldownDuration);
        playerSkills?.SyncCooldownBarToPhoton();
        // Cooldown phase
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            if (playerSkills != null) playerSkills.UpdateSkillCooldownUI(timer, cooldownDuration);
            playerSkills?.SyncCooldownBarToPhoton();
            yield return null;
        }
        if (playerSkills != null) playerSkills.UpdateSkillCooldownUI(cooldownDuration, cooldownDuration);
        playerSkills?.SyncCooldownBarToPhoton();
        isOnCooldown = false;
    }

    public void UnsetInvisibility()
    {
        // Show TP_View and TP_PlayerUI again for others, not for local player
        if (!photonView.IsMine)
        {
            if (playerVisibility != null)
            {
                playerVisibility.SetThirdPersonVisibility(true);
            }
            else
            {
                if (tpViewObject != null) tpViewObject.SetActive(true);
                if (tpPlayerUI != null) tpPlayerUI.SetActive(true);
            }
        }
        // (Layer change now handled by PlayerVisibility)
    }
}
