using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

// The PhotonView is no longer needed on the GameManager.
// You will need to remove it from the GameObject in the Unity Editor.
public class GameManager : MonoBehaviourPunCallbacks
{
    
    public static GameManager Instance { get; private set; }

    [SerializeField]
    GameObject playerPrefab;

    [SerializeField]
    public Transform[] playerSpawners;

    private bool isCursorLocked = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        
        // Ensure player is spawned when the scene loads
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // Add a small delay to ensure everything is initialized
            StartCoroutine(SpawnPlayerDelayed());
        }
    }

    private IEnumerator SpawnPlayerDelayed()
    {
        yield return new WaitForSeconds(0.2f); // A small delay is still good practice.

        // The old logic relying on player properties was causing race conditions on restart.
        // This new logic is simpler and more robust: if our player doesn't exist in the scene, spawn it.
        var myPlayer = FindObjectsOfType<PlayerHealth>().FirstOrDefault(p => p.photonView.IsMine);
        if (myPlayer == null)
        {
            SpawnPlayerAtSpawner(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // The GameManager is specific to the GameScene, so this is the most reliable
        // place to trigger BGM changes when using PhotonNetwork.LoadLevel.
        if (PlayerAudio.Instance != null)
        {
            Debug.Log("<color=orange>GameManager:</color> Starting GameScene, setting BGM volume to 0.1.");
            PlayerAudio.Instance.SetBGMVolume(0.1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorLocked = !isCursorLocked;
            Cursor.lockState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCursorLocked;
        }
    }

    // Photon callbacks
    public override void OnJoinedRoom()
    {
        // Spawn the local player when they join the room
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Set IsAlive property
            var props = new ExitGames.Client.Photon.Hashtable();
            props["IsAlive"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            SpawnPlayerAtSpawnerByIndex();
        }
    }

    // Spawn player at spawner based on their index in the player list
    public void SpawnPlayerAtSpawnerByIndex()
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

        if (myIndex >= 0 && myIndex < playerSpawners.Length)
        {
            Transform spawnPoint = playerSpawners[myIndex];
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError("Player index out of range or spawner not set up!");
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        // Set IsAlive property for new player
        var props = new ExitGames.Client.Photon.Hashtable();
        props["IsAlive"] = true;
        newPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
    }

    public void RegisterVillageText(TextMeshProUGUI text)
    {
    }

    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        // This is now called on the persistent GameManager.
        // It's safe to disconnect and then load the menu.
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Now that we are fully disconnected, load the main menu.
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");

        // AFTER loading the menu, this persistent manager's job is done.
        // Destroying it ensures a fresh one from the new scene is used next time,
        // preventing reference errors.
        Destroy(gameObject);
    }

    // Method to spawn a player at their designated spawner (called when revived)
    public void SpawnPlayerAtSpawner(int playerActorNumber)
    {
        if (playerPrefab != null && playerSpawners.Length >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            int playerIndex = playerActorNumber - 1; // ActorNumber starts at 1
            playerIndex = Mathf.Clamp(playerIndex, 0, playerSpawners.Length - 1);

            Transform spawnPoint = playerSpawners[playerIndex];
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.Log("Place playerPrefab or assign all spawners!");
        }
    }
}
