using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerListManager : MonoBehaviourPunCallbacks
{
    public GameObject otherProfilePrefab; // Assign your OtherProfile prefab in Inspector
    public Transform listParent; // Assign the PlayersList object (with Vertical Layout Group)

    private Dictionary<int, OtherProfileUI> playerEntries = new Dictionary<int, OtherProfileUI>();

    void Start()
    {
        RefreshPlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        Debug.Log($"[PlayerListManager] OnPlayerPropertiesUpdate called for {targetPlayer.NickName} on {PhotonNetwork.LocalPlayer.NickName}. ChangedProps: {string.Join(", ", changedProps.Keys)}");
        float healthPercent = 1f;
        float cooldownPercent = 0f;
        if (targetPlayer.CustomProperties.TryGetValue("HealthPercent", out object hp))
            healthPercent = System.Convert.ToSingle(hp);
        if (targetPlayer.CustomProperties.TryGetValue("SkillCooldownPercent", out object cd))
            cooldownPercent = System.Convert.ToSingle(cd);
        UpdatePlayerInfo(targetPlayer.ActorNumber, healthPercent, cooldownPercent);
    }

    void RefreshPlayerList()
    {
        // Remove old entries
        foreach (Transform child in listParent)
            Destroy(child.gameObject);
        playerEntries.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer) continue; // Skip self

            GameObject entryObj = Instantiate(otherProfilePrefab, listParent);
            OtherProfileUI entry = entryObj.GetComponent<OtherProfileUI>();
            entry.SetPlayer(player);
            playerEntries[player.ActorNumber] = entry;
        }
    }

    // Call this when you want to update health/cooldown for a player
    public void UpdatePlayerInfo(int actorNumber, float healthPercent, float cooldownPercent)
    {
        if (playerEntries.TryGetValue(actorNumber, out var entry))
        {
            entry.UpdateHealth(healthPercent);
            entry.UpdateCooldown(cooldownPercent);
            // Also update the skill image in case SkillIndex changed
            if (entry.player != null && entry.player.CustomProperties.TryGetValue("SkillIndex", out object skillIdxObj))
            {
                int skillIdx = System.Convert.ToInt32(skillIdxObj);
                entry.UpdateSkillImage(skillIdx);
            }
            // Update death panel if IsAlive is present
            if (entry.player != null && entry.player.CustomProperties.TryGetValue("IsAlive", out object isAliveObj))
            {
                bool isAlive = false;
                try { isAlive = System.Convert.ToBoolean(isAliveObj); } catch { }
                entry.UpdateDeathPanel(!isAlive);
            }
        }
    }

    // Call this to manually refresh the player list UI
    public void ForceRefreshPlayerList()
    {
        Debug.Log("[PlayerListManager] ForceRefreshPlayerList called");
        RefreshPlayerList();
    }
} 