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

    [Header("Game Timer")]
    [SerializeField] private float gameDuration = 900f; // 15 minutes in seconds
    private float timeRemaining;
    private bool timerIsRunning = false;
    private TextMeshProUGUI timerText;

    [Header("Game Mode")]
    [SerializeField] private bool isTesting = false;

    [Header("Spawner Visuals")]
    public GameObject[] spawnerVisuals;

    [Header("Guard Spawning")]
    public GameObject[] guardPrefabs;
    public Transform[] guardSpawners;
    public int maxGuards = 40;
    public float spawnInterval = 10f;
    private bool gameHasEnded = false;

    private float guardDebugLogTimer;

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

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GuardSpawnRoutine());
        }
    }

    private IEnumerator GuardSpawnRoutine()
    {
        // Add a short delay to prevent race conditions on game start.
        yield return new WaitForSeconds(1.5f);

        while (PhotonNetwork.InRoom) // This condition will stop the loop when we leave the room.
        {
            // The Master Client is responsible for spawning.
            if (PhotonNetwork.IsMasterClient)
            {
                if (GameObject.FindGameObjectsWithTag("Guard").Length < maxGuards)
                {
                    SpawnGuard();
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnGuard()
    {
        // Add a guard clause to ensure spawners exist.
        if (guardSpawners == null || guardSpawners.Length == 0) return;

        int randomPrefabIndex = Random.Range(0, guardPrefabs.Length);
        int randomSpawnerIndex = Random.Range(0, guardSpawners.Length);

        GameObject guardPrefab = guardPrefabs[randomPrefabIndex];
        Transform spawnPoint = guardSpawners[randomSpawnerIndex];

        // Add a null check to prevent errors when a scene is reloaded.
        // The spawner transform might be destroyed before this coroutine stops.
        if (spawnPoint != null)
        {
            PhotonNetwork.Instantiate(guardPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
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

        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else if (timerIsRunning)
            {
                timerIsRunning = false;
                Debug.Log("Time has run out!");

                // The Master Client is responsible for ending the game.
                if (PhotonNetwork.IsMasterClient && !gameHasEnded)
                {
                    var props = new ExitGames.Client.Photon.Hashtable();
                    props["GameWon"] = true;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                    gameHasEnded = true;
                }
            }
        }

        // Debug log for total guards & Village fall check
        guardDebugLogTimer += Time.deltaTime;
        if (guardDebugLogTimer >= 2f)
        {
            guardDebugLogTimer = 0f;
            // Only log if we are in a room, to prevent errors in the menu.
            if(PhotonNetwork.InRoom)
            {
                Debug.Log($"<color=#FFD700>Real-time guard count:</color> {GameObject.FindGameObjectsWithTag("Guard").Length}");
            }
        }
    }

    public void StartGameTimer()
    {
        if (timerText == null)
        {
            Debug.LogWarning("Timer Text is not assigned. Timer will not be displayed.");
        }
        timeRemaining = gameDuration;
        timerIsRunning = true;
    }

    public void RegisterTimerText(TextMeshProUGUI text)
    {
        timerText = text;
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timerText == null) return;

        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void HideSpawners()
    {
        if (spawnerVisuals != null)
        {
            foreach (GameObject spawner in spawnerVisuals)
            {
                if (spawner != null)
                {
                    spawner.SetActive(false);
                }
            }
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
}
