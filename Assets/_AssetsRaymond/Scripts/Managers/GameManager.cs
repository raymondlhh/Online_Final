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

    [Header("Village Spawning")]
    public GameObject[] villagePrefabs;
    public Transform[] villageSpawners;
    private int totalVillagesToSpawn;
    private int villagesSaved = 0;
    private TextMeshProUGUI villagesText;
    private const string VILLAGES_TO_SPAWN_KEY = "VillagesToSpawn";
    private const string VILLAGES_SAVED_KEY = "VillagesSaved";
    private const string GAMEOVER_REASON_KEY = "GameOverReason";
    private const string GAMEOVER_KEY = "GameOver";

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
            StartCoroutine(SetupVictims());
        }
    }

    private IEnumerator SetupVictims()
    {
        // Wait a frame to ensure all players are in
        yield return null;

        if (isTesting)
        {
            totalVillagesToSpawn = 1;
            Debug.Log("<color=yellow>--- TESTING MODE ENABLED: Spawning only 1 village. ---</color>");
        }
        else
        {
            totalVillagesToSpawn = Random.Range(3, 7); // 3 to 6 villages
        }

        // Store in room properties so all players have the same goal
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[VILLAGES_TO_SPAWN_KEY] = totalVillagesToSpawn;
        props[VILLAGES_SAVED_KEY] = 0; // Initialize saved count
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Get a list of unique spawn points
        List<Transform> availableSpawners = new List<Transform>(villageSpawners);
        for (int i = 0; i < totalVillagesToSpawn; i++)
        {
            if (availableSpawners.Count == 0) break; // Not enough spawners
            if (villagePrefabs.Length == 0)
            {
                Debug.LogError("No village prefabs assigned in GameManager.");
                break;
            }

            int spawnIndex = Random.Range(0, availableSpawners.Count);
            Transform spawnPoint = availableSpawners[spawnIndex];
            availableSpawners.RemoveAt(spawnIndex); // Ensure spawner is unique

            // Randomly select one of the village prefabs to spawn
            GameObject villageToSpawn = villagePrefabs[Random.Range(0, villagePrefabs.Length)];
            PhotonNetwork.Instantiate(villageToSpawn.name, spawnPoint.position, spawnPoint.rotation);
        }
    }

    public void UpdateVillagesSavedCount(int change)
    {
        // This should only ever be called on the Master Client.
        if (!PhotonNetwork.IsMasterClient) return;

        // Get current count from room properties.
        int currentSaved = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VILLAGES_SAVED_KEY, out object saved))
        {
            currentSaved = (int)saved;
        }
    
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[VILLAGES_SAVED_KEY] = currentSaved + change;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
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
                    props[GAMEOVER_KEY] = true;
                    props[GAMEOVER_REASON_KEY] = "TIME_OUT";
                    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                    gameHasEnded = true;
                }
            }
        }

        // Win and Loss Condition Checks (Master Client only)
        if (PhotonNetwork.IsMasterClient && !gameHasEnded)
        {
            // Win if all required villages are saved
            if (totalVillagesToSpawn > 0 && villagesSaved >= totalVillagesToSpawn)
            {
                // Set a room property to signal the win, instead of using an RPC.
                // This avoids the need for a PhotonView on the GameManager.
                var props = new ExitGames.Client.Photon.Hashtable();
                props["GameWon"] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                gameHasEnded = true; // Set locally to prevent sending this multiple times.
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
            
            // Master Client checks for fallen villages periodically
            if (PhotonNetwork.IsMasterClient && !gameHasEnded)
            {
                GameObject[] villages = GameObject.FindGameObjectsWithTag("Village");
                foreach (var village in villages)
                {
                    if (village.transform.position.y < -30f)
                    {
                        var props = new ExitGames.Client.Photon.Hashtable();
                        props[GAMEOVER_KEY] = true;
                        props[GAMEOVER_REASON_KEY] = "VILLAGE_DEAD";
                        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                        gameHasEnded = true; 

                        // Destroy the village so this check doesn't run every frame on them.
                        PhotonNetwork.Destroy(village);
                        break; 
                    }
                }
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
        // Check if village properties have changed
        if (propertiesThatChanged.ContainsKey(VILLAGES_TO_SPAWN_KEY) || propertiesThatChanged.ContainsKey(VILLAGES_SAVED_KEY))
        {
            // Update local values from the definitive source: room properties
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VILLAGES_SAVED_KEY, out object saved))
            {
                villagesSaved = (int)saved;
            }
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VILLAGES_TO_SPAWN_KEY, out object total))
            {
                totalVillagesToSpawn = (int)total;
            }

            // Update the UI
            if (villagesText != null)
            {
                villagesText.text = $"{villagesSaved} / {totalVillagesToSpawn}";
            }
        }

        // Check if the game has been won
        if (propertiesThatChanged.ContainsKey("GameWon"))
        {
            // Find the local player's health script and trigger the succeed screen
            var myPlayer = FindObjectsOfType<PlayerHealth>().FirstOrDefault(p => p.photonView.IsMine);
            if (myPlayer != null)
            {
                myPlayer.TriggerSucceed();
            }
        }

        // Check if the game is over (loss condition)
        if (propertiesThatChanged.ContainsKey(GAMEOVER_KEY))
        {
            var myPlayer = FindObjectsOfType<PlayerHealth>().FirstOrDefault(p => p.photonView.IsMine);
            if (myPlayer != null)
            {
                string reason = (string)PhotonNetwork.CurrentRoom.CustomProperties[GAMEOVER_REASON_KEY];
                myPlayer.TriggerGameOver(reason);
            }
        }
    }

    public void RegisterVillageText(TextMeshProUGUI text)
    {
        villagesText = text;
        Debug.Log("<color=orange>GameManager:</color> Village Text has been registered.");
        // Immediately update the text with the current values, in case this was registered late.
        if (villagesText != null)
        {
            villagesText.text = $"{villagesSaved} / {totalVillagesToSpawn}";
            Debug.Log($"<color=orange>GameManager:</color> Immediately updating registered text to {villagesSaved} / {totalVillagesToSpawn}.");
        }
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
