using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

public class OtherProfileUI : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public GameObject[] skillImages; // Assign each skill image GameObject in order (index matches SkillIndex)
    public Image healthBar;
    public Image cooldownBar;
    public Player player;
    public GameObject deathPanel;

    private int lastSkillIdx = -1;

    public void SetPlayer(Player p)
    {
        player = p;
        playerNameText.text = p.NickName;

        // Hide all skill images first
        foreach (var img in skillImages)
            if (img != null) img.SetActive(false);

        // Show only the one matching SkillIndex
        if (p.CustomProperties.TryGetValue("SkillIndex", out object skillIdxObj))
        {
            int skillIdx = System.Convert.ToInt32(skillIdxObj);
            // Debug.Log($"[OtherProfileUI] {p.NickName} SkillIndex: {skillIdx}");
            lastSkillIdx = skillIdx;
            if (skillIdx >= 0 && skillIdx < skillImages.Length && skillImages[skillIdx] != null)
                skillImages[skillIdx].SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[OtherProfileUI] {p.NickName} has no SkillIndex property!");
        }
        
        // Initialize cooldown bar to empty (skill ready)
        ResetCooldownBar();
    }

    public void UpdateHealth(float healthPercent)
    {
        float hp = 1f;
        try { hp = System.Convert.ToSingle(healthPercent); } catch { }
        if (healthBar != null)
            healthBar.fillAmount = Mathf.Clamp01(hp);
    }

    public void UpdateCooldown(float cooldownPercent)
    {
        float cd = 0f; // Default to empty/ready
        try { cd = System.Convert.ToSingle(cooldownPercent); } catch { }
        // If the value is -1 (uninitialized), treat as empty/ready
        if (cd == -1f) cd = 0f;
        
        // Clamp the value between 0 and 1
        cd = Mathf.Clamp01(cd);
        
        Debug.Log($"[OtherProfileUI] UpdateCooldown called for {playerNameText.text} with value: {cd} (raw: {cooldownPercent}) - 0=Ready, 1=Cannot Use");
        
        if (cooldownBar != null)
            cooldownBar.fillAmount = cd;
    }

    public void UpdateSkillImage(int skillIdx)
    {
        foreach (var img in skillImages)
            if (img != null) img.SetActive(false);
        if (skillIdx >= 0 && skillIdx < skillImages.Length && skillImages[skillIdx] != null)
            skillImages[skillIdx].SetActive(true);
        // Debug.Log($"[OtherProfileUI] UpdateSkillImage called for {playerNameText.text} with SkillIndex: {skillIdx}");
    }

    public void UpdateDeathPanel(bool isDead)
    {
        if (deathPanel != null)
            deathPanel.SetActive(isDead);
        // Debug.Log($"[OtherProfileUI] UpdateDeathPanel called for {playerNameText.text} isDead: {isDead}");
    }

    private void ResetCooldownBar()
    {
        if (cooldownBar != null)
            cooldownBar.fillAmount = 0f; // Start at 0 (skill ready)
    }

    void Start()
    {
        // Hide the death panel by default
        if (deathPanel != null)
            deathPanel.SetActive(false);
        
        // Initialize cooldown bar to empty
        ResetCooldownBar();
    }
} 