using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class ChooseCharacterManager : MonoBehaviour
{
    public Transform[] spawners; // Assign 6 spawner transforms in Inspector
    public string playerPrefabName = "PlayerPrefabFinal"; // Prefab in Resources
    public TMP_Text roomNameText; // Assign in Inspector
    public GameObject[] readyUIs; // Assign P1_ReadyUI, P2_ReadyUI, ... in Inspector

    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignUniqueColorsToAllPlayers();
        }
        HideAllReadyUIExceptHost();
        SpawnPlayerAtSpawner();
        if (roomNameText != null && PhotonNetwork.InRoom)
        {
            roomNameText.text = "Lobby Name: " + PhotonNetwork.CurrentRoom.Name;
        }
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignUniqueColorsToAllPlayers();
        }
    }

    void AssignUniqueColorsToAllPlayers()
    {
        int colorCount = 8; // Number of unique colors
        var takenColors = new HashSet<int>();
        // Gather already assigned colors
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("ColorIndex", out object idx))
                takenColors.Add((int)idx);
        }

        int colorIdx = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            // If player already has a unique color, skip
            if (player.CustomProperties.TryGetValue("ColorIndex", out object idx) && !IsDuplicate((int)idx, player))
            {
                takenColors.Add((int)idx);
                continue;
            }

            // Find the next available color
            while (takenColors.Contains(colorIdx) && colorIdx < colorCount)
                colorIdx++;

            if (colorIdx < colorCount)
            {
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["ColorIndex"] = colorIdx;
                if (!player.CustomProperties.ContainsKey("GenderIndex"))
                    props["GenderIndex"] = 0; // Default to male

                player.SetCustomProperties(props);
                takenColors.Add(colorIdx);
                colorIdx++;
            }
        }
    }

    bool IsDuplicate(int colorIndex, Player currentPlayer)
    {
        int count = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("ColorIndex", out object idx) && (int)idx == colorIndex)
                count++;
        }
        return count > 1;
    }

    void SpawnPlayerAtSpawner()
    {
        Player[] players = PhotonNetwork.PlayerList;
        int myIndex = -1;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                myIndex = i;
                break;
            }
        }

        if (myIndex >= 0 && myIndex < spawners.Length)
        {
            Transform spawnPoint = spawners[myIndex];
            PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError("Player index out of range or spawner not set up!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Returns true if all non-host players are ready
    public bool AllClientsReady()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.IsMasterClient)
            {
                if (!player.CustomProperties.TryGetValue("IsReady", out object val) || !(bool)val)
                    return false;
            }
        }
        return true;
    }

    // Called by host to start the game
    public void OnHostStartGame()
    {
        // Load the next scene or do whatever is needed to start the game
        // Example:
        PhotonNetwork.LoadLevel("TestCharactersScene");
    }

    // Call this to update all ReadyUI objects in the scene
    public void UpdateAllReadyUI()
    {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length && i < readyUIs.Length; i++)
        {
            var player = PhotonNetwork.PlayerList[i];
            var readyUI = readyUIs[i];
            if (readyUI != null)
            {
                bool isReady = player.IsMasterClient || (player.CustomProperties.TryGetValue("IsReady", out object val) && (bool)val);
                readyUI.SetActive(isReady);
            }
        }
    }

    private void HideAllReadyUIExceptHost()
    {
        for (int i = 0; i < readyUIs.Length && i < PhotonNetwork.PlayerList.Length; i++)
        {
            var player = PhotonNetwork.PlayerList[i];
            if (readyUIs[i] != null)
            {
                readyUIs[i].SetActive(player.IsMasterClient);
            }
        }
    }
}

