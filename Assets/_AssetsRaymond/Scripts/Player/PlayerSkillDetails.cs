using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class PlayerSkillDetails : MonoBehaviourPunCallbacks
{
    [Header("Skill UI")]
    public Image CooldownBar; // Assign an Image with fillAmount for the skill bar
    public TextMeshProUGUI timerText; // Shows countdown

    [Header("Skill Timing")]
    [Tooltip("How long the skill stays active when triggered (seconds)")]
    public float activeDuration = 15f;
    [Tooltip("Cooldown time after skill ends (seconds)")]
    public float cooldownDuration = 30f;

    private int skillIndex = -1; // Assigned from SkillIndex property
    private bool isActive = false;
    private bool isOnCooldown = false;
    private float timer = 0f;

    private Coroutine skillRoutine;

    // Decoy Device Skill
    [Header("Decoy Device Skill")]
    public bool isBlackHole = false; // Set true if this skill is DecoyDevice
    public GameObject BlackHole; // Assign in inspector or via hierarchy
    public GameObject WeaponCrosshair; // Assign in inspector or via hierarchy
    public float holeMoveForce = 10f; // How fast the decoy moves forward
    private bool holeReadyToThrow = false;

    [Header("Decoy Device VFX")]
    public GameObject BlackHoleVFXPrefab; // Assign the VFX prefab in inspector
    public Camera playerCamera; // Assign the player's camera in inspector

    [Header("Testing")]
    public bool isTesting = false;
    [Tooltip("If isTesting is true, use this index for the skill instead of the player's property")]
    public int testSkillIndex = 0;

    void Start()
    {
        // Get the assigned skill index from the player's properties
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (isTesting)
        {
            skillIndex = testSkillIndex;
        }
        else if (pv != null && pv.Owner != null && pv.Owner.CustomProperties.ContainsKey("SkillIndex"))
        {
            skillIndex = (int)pv.Owner.CustomProperties["SkillIndex"];
        }
        // Assume DecoyDevice is skillIndex 0 (change as needed)
        isBlackHole = (skillIndex == 0);
        ResetUI();
        // Set initial cooldown bar to full (ready) and sync to Photon
        SyncCooldownBarToPhoton();
        // Hide DecoyDevice and WeaponCrosshair at start
        if (BlackHole != null) BlackHole.SetActive(false);
        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Activate skill
        if (Input.GetKeyDown(KeyCode.E) && !isActive && !isOnCooldown)
        {
            if (skillRoutine != null) StopCoroutine(skillRoutine);
            skillRoutine = StartCoroutine(SkillActiveAndCooldownRoutine());
            // If DecoyDevice, show DecoyDevice and WeaponCrosshair, ready to throw
            if (isBlackHole)
            {
                if (BlackHole != null) BlackHole.SetActive(true);
                if (WeaponCrosshair != null) WeaponCrosshair.SetActive(true);
                holeReadyToThrow = true;
            }
        }
    }

    private IEnumerator SkillActiveAndCooldownRoutine()
    {
        isActive = true;
        isOnCooldown = false;
        timer = activeDuration;
        bool decoyThrown = false;
        // If DecoyDevice, show it (if not already thrown)
        if (isBlackHole && BlackHole != null && !BlackHole.activeSelf)
            BlackHole.SetActive(true);
        if (isBlackHole && WeaponCrosshair != null && !WeaponCrosshair.activeSelf)
            WeaponCrosshair.SetActive(true);
        holeReadyToThrow = true;
        while (timer > 0f && isActive && !decoyThrown)
        {
            timer -= Time.deltaTime;
            UpdateUI(timer, activeDuration, true);
            SyncCooldownBarToPhoton();
            // Check for left mouse click to throw decoy
            if (isBlackHole && holeReadyToThrow && Input.GetMouseButtonDown(0))
            {
                if (playerCamera != null && BlackHoleVFXPrefab != null)
                {
                    Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100f))
                    {
                        Instantiate(BlackHoleVFXPrefab, hit.point, Quaternion.identity);
                        if (BlackHole != null) BlackHole.SetActive(false);
                        if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
                        holeReadyToThrow = false;
                        decoyThrown = true;
                        break; // Immediately end active phase and go to cooldown
                    }
                }
            }
            yield return null;
        }
        isActive = false;
        UpdateUI(0, activeDuration, true);
        SyncCooldownBarToPhoton();
        // Always hide DecoyDevice and WeaponCrosshair at end of active phase
        if (isBlackHole)
        {
            if (BlackHole != null) BlackHole.SetActive(false);
            if (WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
            holeReadyToThrow = false;
        }
        // Start cooldown for 30s
        isOnCooldown = true;
        timer = 0f;
        while (timer < cooldownDuration)
        {
            timer += Time.deltaTime;
            UpdateUI(timer, cooldownDuration, false);
            SyncCooldownBarToPhoton();
            yield return null;
        }
        isOnCooldown = false;
        UpdateUI(cooldownDuration, cooldownDuration, false);
        SyncCooldownBarToPhoton();
    }

    private void UpdateUI(float t, float max, bool isActivePhase)
    {
        if (CooldownBar != null)
        {
            if (isActivePhase)
                CooldownBar.fillAmount = t / max; // Decrease from 1 to 0
            else
                CooldownBar.fillAmount = t / max; // Increase from 0 to 1
        }
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(isActivePhase ? t : (max - t));
            timerText.text = seconds.ToString();
        }
    }

    private void SyncCooldownBarToPhoton()
    {
        if (photonView != null && photonView.IsMine && CooldownBar != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["SkillCooldownPercent"] = CooldownBar.fillAmount;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    private void ResetUI()
    {
        if (CooldownBar != null) CooldownBar.fillAmount = 1f;
        if (timerText != null) timerText.text = "";
        if (isBlackHole && BlackHole != null) BlackHole.SetActive(false);
        if (isBlackHole && WeaponCrosshair != null) WeaponCrosshair.SetActive(false);
    }
}
