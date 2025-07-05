using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : MonoBehaviourPunCallbacks
{
    [Header("Health Related Stuff")]
    public float startHealth = 100;
    
    [Header("UI Elements")]
    [SerializeField] private Image TPHealth;    // Third person healthbar
    [SerializeField] private Image FPHealth;    // First person healthbar
    [SerializeField] private GameObject deadPanel;
    [SerializeField] private GameObject succeedPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverDescriptionText;
    
    [Header("GameObjects for Visibility Control")]
    [SerializeField] private GameObject tpView; // Third-person view (the model)

    [Header("Damage Effect")]
    [SerializeField] private Volume damageVolume;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color reviveColor = Color.green;
    private Vignette vignette;
    private Coroutine damageEffectCoroutine;

    private float health;
    private Animator animator;
    private bool isLocalPlayer;
    private bool isInvulnerable = false;
    public bool IsDowned { get; private set; } = false;

    // Component references
    private PlayerMovement movementController;
    private PlayerAttack playerShoot;
    private PlayerSkillDetails[] skillDetails;
    private int playerLayer;
    private int cloakedPlayerLayer;
    private bool gameOverTriggered = false; // Prevents multiple triggers

    void Awake()
    {
        isLocalPlayer = photonView.IsMine;
        
        // Disable FP UI elements for non-local players
        if (!isLocalPlayer && FPHealth != null)
        {
            Transform fpUI = FPHealth.transform.root.Find("FP_PlayerUI");
            if (fpUI != null)
            {
                fpUI.gameObject.SetActive(false);
            }
        }

        // Get layer integers
        playerLayer = LayerMask.NameToLayer("Player");
        cloakedPlayerLayer = LayerMask.NameToLayer("CloakedPlayer");
    }

    void Start()
    {
        health = startHealth;
        animator = GetComponent<Animator>();
        movementController = GetComponent<PlayerMovement>();
        playerShoot = GetComponent<PlayerAttack>();
        skillDetails = GetComponentsInChildren<PlayerSkillDetails>();
        UpdateHealthBars();

        if (isLocalPlayer)
        {
            // When this player object is created, authoritatively set its status to alive.
            var props = new ExitGames.Client.Photon.Hashtable();
            props["IsAlive"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // Setup post-processing effect only for the local player
        if (photonView.IsMine && damageVolume != null)
        {
            damageVolume.profile.TryGet(out vignette);
            
            if (vignette != null)
            {
                vignette.intensity.Override(0f); // Start with no effect
            }
        }
        else if (damageVolume != null)
        {
            // Deactivate the volume for non-local players to be tidy.
            damageVolume.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    public void TakeDamage(float _damage, PhotonMessageInfo info)
    {
        if (isInvulnerable)
        {
            Debug.Log($"Player {photonView.Owner.NickName} is invulnerable and took no damage.");
            return;
        }

        health -= _damage;
        health = Mathf.Max(0, health); // Prevent negative health
        Debug.Log($"Player {photonView.Owner.NickName} took damage. Health: {health}");

        UpdateHealthBars();

        if (health <= 0f)
        {
            Die();
            Debug.Log(info.Sender.NickName + " killed " + info.photonView.Owner.NickName);
        }

        // Trigger damage effect for local player
        if (photonView.IsMine)
        {
            if (damageEffectCoroutine != null) StopCoroutine(damageEffectCoroutine);
            damageEffectCoroutine = StartCoroutine(DamageEffect());
        }
    }

    [PunRPC]
    public void TakeDamageFromAI(float _damage)
    {
        if (isInvulnerable)
        {
            Debug.Log($"Player {photonView.Owner.NickName} is invulnerable and took no damage.");
            return;
        }

        health -= _damage;
        health = Mathf.Max(0, health); // Prevent negative health
        Debug.Log($"Player {photonView.Owner.NickName} took AI damage. Health: {health}");

        UpdateHealthBars();

        if (health <= 0f)
        {
            Die();
            Debug.Log("An AI killed " + photonView.Owner.NickName);
        }

        // Trigger damage effect for local player
        if (photonView.IsMine)
        {
            if (damageEffectCoroutine != null) StopCoroutine(damageEffectCoroutine);
            damageEffectCoroutine = StartCoroutine(DamageEffect());
        }
    }

    [PunRPC]
    void SyncHealth(float newHealth)
    {
        health = newHealth;
        UpdateHealthBars();
    }

    public void RegainHealth()
    {
        photonView.RPC("RegainHealthRPC", RpcTarget.All);
    }

    [PunRPC]
    public void RegainHealthRPC()
    {
        health = startHealth;
        UpdateHealthBars();
    }

    private void UpdateHealthBars()
    {
        float healthPercentage = health / startHealth;
        
        // Update Third Person healthbar for everyone
        if (TPHealth != null)
        {
            TPHealth.fillAmount = healthPercentage;
            Debug.Log($"Updating TP healthbar for {photonView.Owner.NickName}: {healthPercentage}");
        }
            
        // Update First Person healthbar only for local player
        if (isLocalPlayer && FPHealth != null)
        {
            FPHealth.fillAmount = healthPercentage;
            Debug.Log($"Updating FP healthbar: {healthPercentage}");
        }
    }

    public void ActivateBloodLock(float duration)
    {
        if (photonView.IsMine)
        {
            StartCoroutine(BloodLockCoroutine(duration));
        }
    }

    private IEnumerator BloodLockCoroutine(float duration)
    {
        photonView.RPC("SetInvulnerable", RpcTarget.All, true);
        yield return new WaitForSeconds(duration);
        photonView.RPC("SetInvulnerable", RpcTarget.All, false);
    }

    [PunRPC]
    private void SetInvulnerable(bool state)
    {
        isInvulnerable = state;
        Debug.Log($"Player {photonView.Owner.NickName} invulnerability set to: {state}");
    }

    void Die()
    {
        if (isLocalPlayer)
        {
            // Tell all clients that this player is now dead.
            photonView.RPC("GoDown", RpcTarget.All);
            // Show the deadPanel if not already in game over
            if (deadPanel != null && (gameOverPanel == null || !gameOverPanel.activeInHierarchy))
            {
                deadPanel.SetActive(true);
            }
        }
    }

    [PunRPC]
    private void GoDown()
    {
        IsDowned = true;

        if (animator != null)
            animator.SetBool("IsDead", true);

        if (isLocalPlayer)
        {
            // Set layer to cloaked so they can't be targeted while down
            SetLayerRecursively(gameObject, cloakedPlayerLayer);

            // Set custom property to dead
            var props = new ExitGames.Client.Photon.Hashtable();
            props["IsAlive"] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            // This player is the one who died. Run the local "downed" sequence.
            StartCoroutine(EnterDownedState());
        }
    }

    private void CheckForGameOver()
    {
        // This check only runs on the Master Client for authority.
        if (!PhotonNetwork.IsMasterClient) return;

        // Check if game has already ended
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameOver")) return;

        // Check if all players are dead 
        bool allPlayersDead = true;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            object isAlive;
            if (player.CustomProperties.TryGetValue("IsAlive", out isAlive))
            {
                if ((bool)isAlive)
                {
                    allPlayersDead = false;
                    break;
                }
            }
            else
            {
                // Property not set yet, assume they are alive.
                allPlayersDead = false;
                break;
            }
        }

        // If all players are dead, trigger game over
        if (allPlayersDead)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["GameOver"] = true;
            props["GameOverReason"] = "ALL_DEBUGGERS_DEAD";
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // When a player's "IsAlive" status changes, check for game over.
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("IsAlive"))
        {
            CheckForGameOver();
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // NOTE: The GameOver check is now handled by GameManager's OnRoomPropertiesUpdate
        // to centralize the logic and prevent race conditions.
        // This script's OnRoomPropertiesUpdate is now only for things the PlayerHealth script itself needs to react to.
    }

    public void TriggerGameOver(string reason)
    {
        // Prevent this from running multiple times if properties update in quick succession
        if (gameOverTriggered || !isLocalPlayer) return;
        gameOverTriggered = true;

        // This is called on the local client to show their own Game Over screen.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverDescriptionText != null)
        {
            switch (reason)
            {
                case "TIME_OUT":
                    gameOverDescriptionText.text = "TIME OUT";
                    break;
                case "VILLAGE_DEAD":
                    gameOverDescriptionText.text = "VILLAGE DIED";
                    break;
                case "ALL_DEBUGGERS_DEAD":
                default:
                    gameOverDescriptionText.text = "ALL DEBUGGERS DEAD";
                    break;
            }
        }
        
        // Also hide the dead panel if it's active
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }

        // Disable player controls and interactions
        if (movementController != null) movementController.enabled = false;
        if (playerShoot != null) playerShoot.enabled = false;
        foreach(var skill in skillDetails)
        {
            skill.enabled = false;
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TriggerSucceed()
    {
        if (!isLocalPlayer) return;

        if (succeedPanel != null)
        {
            succeedPanel.SetActive(true);
        }
        
        // As requested, hide the dead panel if it was active.
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }

        // Disable player controls
        if (movementController != null) movementController.enabled = false;
        if (playerShoot != null) playerShoot.enabled = false;

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator EnterDownedState()
    {
        // 1. Disable controls
        movementController.CanMove = false;
        movementController.CanLook = false;
        playerShoot.enabled = false;
        foreach(var skill in skillDetails)
        {
            skill.enabled = false;
        }

        // 2. Teleport to spawner and stay there
        if (GameManager.Instance != null)
        {
            Transform spawnPoint = GameManager.Instance.playerSpawners[photonView.Owner.ActorNumber - 1];
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // 3. Show Dead Panel UI, but only if the game isn't already over.
        if (gameOverPanel != null && !gameOverPanel.activeInHierarchy)
        {
            if (deadPanel != null) deadPanel.SetActive(true);
        }

        // 5. Player stays at spawner waiting for revive
        yield return null;
    }

    [PunRPC]
    public void Revive()
    {
        StartCoroutine(ReviveSequence());
    }

    IEnumerator ReviveSequence()
    {
        IsDowned = false;

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }

        // Wait a frame for the animator to begin its transition
        yield return null;

        // Now, show the TP_View only for remote players
        if (tpView != null)
        {
           tpView.SetActive(!isLocalPlayer);
        }

        if (isLocalPlayer)
        {
            // Set layer back to default Player layer
            SetLayerRecursively(gameObject, playerLayer);

            // Set custom property to alive
            var props = new ExitGames.Client.Photon.Hashtable();
            props["IsAlive"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            // Hide dead panel
            if (deadPanel != null) deadPanel.SetActive(false);
            
            // Re-enable controls
            movementController.CanMove = true;
            movementController.CanLook = true;
            playerShoot.enabled = true;
            foreach (var skill in skillDetails)
            {
                skill.enabled = true;
            }
        }
        
        // Restore health
        RegainHealth();

        // Trigger revive effect for local player
        if (isLocalPlayer)
        {
            if (damageEffectCoroutine != null) StopCoroutine(damageEffectCoroutine);
            damageEffectCoroutine = StartCoroutine(ReviveEffect());
        }
    }

    private IEnumerator DamageEffect()
    {
        Vignette vignette;
        if (!TryGetVignette(out vignette)) yield break;

        vignette.color.Override(damageColor);
        float duration = 0.4f;
        float maxIntensity = 0.5f;

        // Fade In
        float timer = 0f;
        while (timer < duration / 2)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxIntensity, timer / (duration / 2));
            timer += Time.deltaTime;
            yield return null;
        }
        vignette.intensity.value = maxIntensity;

        // Fade Out
        timer = 0f;
        while (timer < duration / 2)
        {
            vignette.intensity.value = Mathf.Lerp(maxIntensity, 0f, timer / (duration / 2));
            timer += Time.deltaTime;
            yield return null;
        }

        vignette.intensity.value = 0f;
    }

    private IEnumerator ReviveEffect()
    {
        Vignette vignette;
        if (!TryGetVignette(out vignette)) yield break;

        vignette.color.Override(reviveColor);
        float duration = 0.6f; // A slightly longer effect for revives
        float maxIntensity = 0.4f;

        // Fade In
        float timer = 0f;
        while (timer < duration / 2)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxIntensity, timer / (duration / 2));
            timer += Time.deltaTime;
            yield return null;
        }
        vignette.intensity.value = maxIntensity;

        // Fade Out
        timer = 0f;
        while (timer < duration / 2)
        {
            vignette.intensity.value = Mathf.Lerp(maxIntensity, 0f, timer / (duration / 2));
            timer += Time.deltaTime;
            yield return null;
        }
        
        vignette.intensity.value = 0f;
    }

    private bool TryGetVignette(out Vignette vignette)
    {
        vignette = null;
        if (damageVolume == null)
        {
            Transform visualEffectsTransform = transform.Find("VisualEffects");
            if (visualEffectsTransform == null) return false;
            damageVolume = visualEffectsTransform.GetComponent<Volume>();
            if (damageVolume == null) return false;
        }
        
        return damageVolume.profile.TryGet(out vignette);
    }

    // Helper function to apply a layer to a GameObject and all its children.
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // Don't change the layer of the VisualEffects object or its children.
        if (obj.name == "VisualEffects")
        {
            return;
        }

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine && transform.position.y < -30f)
        {
            if (health > 0)
            {
                health = 0;
                UpdateHealthBars();
                Die();
                // Show the deadPanel if not already in game over
                if (deadPanel != null && (gameOverPanel == null || !gameOverPanel.activeInHierarchy))
                {
                    deadPanel.SetActive(true);
                }
            }
        }
    }

    public void ExitToMainMenu()
    {
        if (PlayerAudio.Instance != null)
        {
            PlayerAudio.Instance.PlaySFX("Button Pressed");
        }

        // Tell the persistent GameManager to handle the exit process.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LeaveGame();
        }
        else
        {
            // Fallback in case the GameManager can't be found.
            PhotonNetwork.Disconnect();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }

    private void ResetVisualsForMenu()
    {
        // This method resets the player's visual state for returning to a menu-like screen
        // without affecting their networked "IsAlive" status, which will be reset on scene reload.
        IsDowned = false;

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }

        // Make sure the player model is visible again for the character selection screen.
        if (tpView != null)
        {
           tpView.SetActive(true);
        }
    }

    public void ForceDie()
    {
        if (health > 0)
        {
            // Take enough damage to trigger death sequence, ensuring all logic runs.
            // Using TakeDamage is better than just setting health = 0 and calling Die(),
            // as it will correctly handle damage effects.
            TakeDamage(startHealth * 2, new PhotonMessageInfo()); // Overkill damage to ensure death
        }
    }
}
