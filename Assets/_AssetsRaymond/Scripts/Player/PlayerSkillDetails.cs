using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class PlayerSkillDetails : MonoBehaviour
{
    [Header("Skill Settings")]
    public KeyCode skillKey;
    public float skillCooldown = 10f;
    public bool isTimedSwordBlade = false;
    public bool isTimedBloodLock = false;
    public bool isTimedHighJump = false;
    public bool isTimedSlowFall = false;
    public bool isTimedGhostCloak = false;
    public bool isShadowSwapSkill = false;
    public bool isConnectMovementSkill = false;
    public float skillDuration = 15f;

    [Header("UI")]
    public Image cooldownImage;
    public TextMeshProUGUI cooldownText;

    [Header("Skill-Specific UI")]
    public GameObject bloodLockUI;
    public GameObject ghostCloakUI;
    public TextMeshProUGUI durationText;
    public GameObject shadowSwapFXPrefab;

    private float currentCooldown = 0f;
    private PlayerAttack playerShoot;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovementController;
    private PlayerVisibility playerVisibilityController;
    private PlayerConnector playerConnector;

    private bool slowFallActive = false;
    private Coroutine timedSkillUICoroutine;

    private bool _shadowMarkerPlaced = false;
    private GameObject _currentShadowFX;
    private Vector3 _shadowSwapPosition;
    private Coroutine _shadowMarkerCoroutine;

    private bool isConnectSkillActive = false;

    public bool IsOnCooldown => currentCooldown > 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = 0;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }

        if (ghostCloakUI != null)
        {
            ghostCloakUI.SetActive(false);
        }

        if (durationText != null)
        {
            durationText.gameObject.SetActive(false);
        }

        playerShoot = GetComponentInParent<PlayerAttack>();
        playerHealth = GetComponentInParent<PlayerHealth>();
        playerMovementController = GetComponentInParent<PlayerMovement>();
        playerVisibilityController = GetComponentInParent<PlayerVisibility>();
        playerConnector = GetComponentInParent<PlayerConnector>();

        if (isShadowSwapSkill && shadowSwapFXPrefab == null)
        {
            Debug.LogError("Shadow Swap skill requires a FX Prefab assigned.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(skillKey))
        {
            if (isConnectMovementSkill && isConnectSkillActive)
            {
                if (playerConnector != null)
                {
                    playerConnector.CancelConnection();
                }
                isConnectSkillActive = false;
                
                if (timedSkillUICoroutine != null) StopCoroutine(timedSkillUICoroutine);
                if (durationText != null) durationText.gameObject.SetActive(false);
            }
            else if (isShadowSwapSkill)
            {
                HandleShadowSwap();
            }
            else if (isTimedSlowFall && slowFallActive)
            {
                playerMovementController.DeactivateSlowFall();
                if (timedSkillUICoroutine != null)
                {
                    StopCoroutine(timedSkillUICoroutine);
                }
                if (durationText != null) durationText.gameObject.SetActive(false);
                
                slowFallActive = false;
                currentCooldown = 0f;
                if (cooldownImage != null) cooldownImage.fillAmount = 0;
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
            }
            else if (!IsOnCooldown)
            {
                UseSkill();
            }
        }

        if (IsOnCooldown)
        {
            currentCooldown -= Time.deltaTime;
            
            if (cooldownImage != null)
            {
                cooldownImage.fillAmount = currentCooldown / skillCooldown;
            }

            if (cooldownText != null)
            {
                cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
            }

            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
                if (cooldownImage != null)
                {
                    cooldownImage.fillAmount = 0f;
                }
                if (cooldownText != null)
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UseSkill()
    {
        Debug.Log($"UseSkill called on {gameObject.name}. isTimedSwordSkill is set to: {isTimedSwordBlade}");

        bool isTimedSkill = false;
        if (isTimedSwordBlade && playerShoot != null)
        {
            playerShoot.ActivateSword(skillDuration);
            isTimedSkill = true;
        }
        if (isTimedBloodLock && playerHealth != null)
        {
            playerHealth.ActivateBloodLock(skillDuration);
            isTimedSkill = true;
        }
        if (isTimedHighJump && playerMovementController != null)
        {
            playerMovementController.ActivateHighJump(skillDuration);
            isTimedSkill = true;
        }
        if (isTimedSlowFall && playerMovementController != null)
        {
            playerMovementController.ActivateSlowFall(skillDuration);
            slowFallActive = true;
            isTimedSkill = true;

            // If a player is connected, apply slow fall to them too
            if (playerConnector != null && playerConnector.ConnectedPlayerMovement != null)
            {
                var connectedPlayerView = playerConnector.ConnectedPlayerMovement.GetComponent<PhotonView>();
                if (connectedPlayerView != null)
                {
                    connectedPlayerView.RPC("SetKinematicState", RpcTarget.All, true);
                }
            }
        }
        if (isTimedGhostCloak && playerVisibilityController != null)
        {
            playerVisibilityController.ActivateGhostCloak(skillDuration);
            isTimedSkill = true;
        }

        if (isConnectMovementSkill)
        {
            if (playerConnector != null)
            {
                var connectResult = playerConnector.TryConnect(skillDuration);
                
                if (connectResult == PlayerConnector.ConnectionResult.Success)
                {
                    isConnectSkillActive = true;
                    timedSkillUICoroutine = StartCoroutine(HandleTimedSkillUI(skillDuration));
                    StartCooldown();
                }
            }
        }
        else
        {
            if (isTimedSkill)
            {
                timedSkillUICoroutine = StartCoroutine(HandleTimedSkillUI(skillDuration));
            }

            if (isTimedSkill || isTimedGhostCloak)
            {
                StartCooldown();
            }
        }
    }

    private void HandleShadowSwap()
    {
        if (!_shadowMarkerPlaced)
        {
            if (IsOnCooldown) return;

            _shadowSwapPosition = playerMovementController.transform.position;
            _currentShadowFX = PhotonNetwork.Instantiate(shadowSwapFXPrefab.name, _shadowSwapPosition, Quaternion.identity);
            _shadowMarkerPlaced = true;
            
            _shadowMarkerCoroutine = StartCoroutine(ShadowMarkerLifetime(skillDuration));
            timedSkillUICoroutine = StartCoroutine(HandleTimedSkillUI(skillDuration));
        }
        else
        {
            playerVisibilityController.Teleport(_shadowSwapPosition);
            if (_currentShadowFX != null) PhotonNetwork.Destroy(_currentShadowFX);

            if (_shadowMarkerCoroutine != null) StopCoroutine(_shadowMarkerCoroutine);
            if (timedSkillUICoroutine != null) StopCoroutine(timedSkillUICoroutine);
            
            _shadowMarkerPlaced = false;
            _currentShadowFX = null;
            _shadowMarkerCoroutine = null;
            timedSkillUICoroutine = null;

            if (durationText != null) durationText.gameObject.SetActive(false);
            
            StartCooldown();
        }
    }

    private IEnumerator ShadowMarkerLifetime(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (_shadowMarkerPlaced)
        {
            if (_currentShadowFX != null) PhotonNetwork.Destroy(_currentShadowFX);
            
            _shadowMarkerPlaced = false;
            _currentShadowFX = null;
            _shadowMarkerCoroutine = null;
            timedSkillUICoroutine = null;

            if (cooldownImage != null) cooldownImage.fillAmount = 0;
            if (durationText != null) durationText.gameObject.SetActive(false);
        }
    }

    private void StartCooldown()
    {
        currentCooldown = skillCooldown;
        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = 1f;
        }
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
        }
    }

    private IEnumerator HandleTimedSkillUI(float duration)
    {
        if (bloodLockUI != null && isTimedBloodLock)
        {
            bloodLockUI.SetActive(true);
        }
        if (ghostCloakUI != null && isTimedGhostCloak)
        {
            ghostCloakUI.SetActive(true);
        }
        if (durationText != null)
        {
            durationText.gameObject.SetActive(true);
        }

        float remainingDuration = duration;
        while (remainingDuration > 0f)
        {
            if (durationText != null)
            {
                durationText.text = Mathf.CeilToInt(remainingDuration).ToString();
            }
            remainingDuration -= Time.deltaTime;
            yield return null;
        }

        if (isConnectMovementSkill)
        {
            isConnectSkillActive = false;
        }

        if (bloodLockUI != null && isTimedBloodLock)
        {
            bloodLockUI.SetActive(false);
        }
        if (ghostCloakUI != null && isTimedGhostCloak)
        {
            ghostCloakUI.SetActive(false);
        }
        if (durationText != null)
        {
            durationText.gameObject.SetActive(false);
        }
        
        if (isTimedSlowFall)
        {
            // If a player was connected, reset their physics
            if (playerConnector != null && playerConnector.ConnectedPlayerMovement != null)
            {
                var connectedPlayerView = playerConnector.ConnectedPlayerMovement.GetComponent<PhotonView>();
                if (connectedPlayerView != null)
                {
                    connectedPlayerView.RPC("SetKinematicState", RpcTarget.All, false);
                }
            }
            slowFallActive = false;
        }
        timedSkillUICoroutine = null;
    }

    /// <summary>
    /// Called by PlayerConnector when the victim connection is broken externally (e.g. victim saved).
    /// </summary>
    public void CancelConnectSkillUI()
    {
        if (isConnectSkillActive)
        {
            isConnectSkillActive = false;
            if (timedSkillUICoroutine != null)
            {
                StopCoroutine(timedSkillUICoroutine);
                timedSkillUICoroutine = null;
            }
            if (durationText != null)
            {
                durationText.gameObject.SetActive(false);
            }
        }
    }
}
