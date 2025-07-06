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

    [Header("Skill UI")]
    public Image CooldownBar; // Assign an Image with fillAmount for the skill bar
    public TextMeshProUGUI CooldownTime; // Shows countdown

    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 10f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

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
                playerVisibility.photonView.RPC("SetInvisibilityRelay", Photon.Pun.RpcTarget.All);
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
        // Set layer to CloakedPlayer
        if (tpViewObject != null) SetLayerRecursively(tpViewObject, cloakedPlayerLayer);
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

    private void SetCooldownPercent(float percent)
    {
        if (photonView.IsMine)
        {
            if (CooldownBar != null) CooldownBar.fillAmount = percent;
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = percent;
            Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else
        {
            if (CooldownBar != null) CooldownBar.fillAmount = percent;
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
            SetCooldownPercent(timer / activeDuration);
            yield return null;
        }
        SetCooldownPercent(0f);
        if (photonView.IsMine && invisibilityPanel != null)
            invisibilityPanel.SetActive(false);
        isInvisible = false;
        if (playerVisibility != null)
            playerVisibility.photonView.RPC("UnsetInvisibilityRelay", Photon.Pun.RpcTarget.All);
        // Cooldown phase
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            SetCooldownPercent(timer / cooldownDuration);
            yield return null;
        }
        SetCooldownPercent(1f);
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
        // Set layer back to Player
        if (tpViewObject != null) SetLayerRecursively(tpViewObject, LayerMask.NameToLayer("Player"));
    }
}
