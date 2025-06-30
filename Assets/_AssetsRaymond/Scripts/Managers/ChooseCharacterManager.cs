using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class ChooseCharacterManager : MonoBehaviour
{
    public Transform[] spawners; // Assign 6 spawner transforms in Inspector
    public string playerPrefabName = "PlayerPrefabFinal"; // Prefab in Resources
    public TMP_Text roomNameText; // Assign in Inspector

    // Start is called before the first frame update
    void Start()
    {
        SpawnPlayerAtSpawner();
        if (roomNameText != null && PhotonNetwork.InRoom)
        {
            roomNameText.text = "Lobby Name: " + PhotonNetwork.CurrentRoom.Name;
        }
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
}
