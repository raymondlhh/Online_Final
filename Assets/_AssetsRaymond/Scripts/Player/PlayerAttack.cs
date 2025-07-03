using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

public class PlayerAttack : MonoBehaviourPunCallbacks
{
    public Camera FPS_Camera;
    public GameObject hitEffectPrefab;

    [Header("Animator Settings")]
    public Animator FPAnimator;
    public Animator TPAnimator; 
    

    [Header("Weapon Settings")]
    public float damage = 10f;
    public int maxAmmo = 30;
    public int currentAmmo;
    public float reloadTime = 2f;
    public bool isReloading = false;
    public float fireRate = 0.2f;
    private float nextFireTime = 0f;
    public TMP_Text ammoText;

    // Add weapon references
    [Header("Weapons - First Person")]
    public GameObject Rifle_FP;
    public GameObject Shotgun_FP;
    public GameObject SMG_FP;

    [Header("Weapons - Third Person")]
    public GameObject Rifle_TP;
    public GameObject Shotgun_TP;
    public GameObject SMG_TP;

    [Header("Weapons - Sword")]
    public GameObject Sword_FP;
    public GameObject Sword_TP;

    [Header("Sword Settings")]
    public float swordDamage = 50f;
    public float swordAttackRange = 3f;
    public float swordAttackRate = 1f;

    private int currentWeaponIndex = 0; // 0: Rifle, 1: Shotgun, 2: SMG
    private GameObject[] weaponsFP;
    private GameObject[] weaponsTP;
    
    // Add arrays to track ammo for each weapon
    private int[] currentAmmoPerWeapon;
    private int[] maxAmmoPerWeapon;

    private PlayerHealth playerHealth;

    private Weapon currentWeaponData;

    private bool isChanging = false;  // Add this field
    public float weaponSwitchTime = 0.5f;  // Add this to control switch animation duration

    private bool isSwordActive = false;
    private float nextSwordAttackTime = 0f;
    private Coroutine swordActivationCoroutine;
    private Coroutine reloadCoroutine;

    void Awake()
    {
        // Initialize weapons array
        weaponsFP = new GameObject[] { Rifle_FP, Shotgun_FP, SMG_FP };
        weaponsTP = new GameObject[] { Rifle_TP, Shotgun_TP, SMG_TP };

        // Initialize ammo arrays
        currentAmmoPerWeapon = new int[3];
        maxAmmoPerWeapon = new int[3];
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set initial max ammo for each weapon from their Weapon components
        for (int i = 0; i < weaponsFP.Length; i++)
        {
            if (weaponsFP[i] != null)
            {
                var weaponData = weaponsFP[i].GetComponent<Weapon>();
                if (weaponData != null)
                {
                    maxAmmoPerWeapon[i] = weaponData.maxAmmo;
                    currentAmmoPerWeapon[i] = weaponData.maxAmmo; // Start with full ammo
                }
            }
        }

        SelectWeapon(currentWeaponIndex);
        playerHealth = GetComponent<PlayerHealth>();

        // Only setup and show UI elements for the local player
        if (photonView.IsMine)
        {
            // Find AmmoText specifically by name in FP_PlayerUI
            Transform fpPlayerUI = transform.Find("FP_PlayerUI");
            if (fpPlayerUI != null)
            {
                // Look for a child named "AmmoText" specifically
                Transform ammoTextTransform = fpPlayerUI.Find("AmmoText");
                if (ammoTextTransform != null)
                {
                    ammoText = ammoTextTransform.GetComponent<TMP_Text>();
                    Debug.Log("Found AmmoText: " + ammoText.name);
                }
                else
                {
                    Debug.LogError("Could not find AmmoText in FP_PlayerUI");
                }
            }
            else
            {
                Debug.LogError("Could not find FP_PlayerUI");
            }
            UpdateAmmoUI();
        }
        else
        {
            // Disable ammo UI for non-local players
            if (ammoText != null)
            {
                ammoText.gameObject.SetActive(false);
            }
        }

        if (photonView.IsMine)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["WeaponIndex"] = currentWeaponIndex;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        if (photonView.Owner.CustomProperties.ContainsKey("WeaponIndex"))
        {
            currentWeaponIndex = (int)photonView.Owner.CustomProperties["WeaponIndex"];
            ApplyWeaponVisual(currentWeaponIndex);
        }
        else
        {
            ApplyWeaponVisual(currentWeaponIndex);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "ChooseCharacterScene")
            return;

        if (!photonView.IsMine) return;

        if (isSwordActive)
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= nextSwordAttackTime)
            {
                SwordAttack();
                nextSwordAttackTime = Time.time + swordAttackRate;
            }
        }
        else
        {
            // Weapon switching by key
            if (Input.GetKeyDown(KeyCode.Alpha1)) { SetWeaponIndex(0); }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { SetWeaponIndex(1); }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { SetWeaponIndex(2); }

            // Mouse wheel switching
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                SetWeaponIndex((currentWeaponIndex + 2) % 3); // previous
            }
            else if (scroll < 0f)
            {
                SetWeaponIndex((currentWeaponIndex + 1) % 3); // next
            }

            // Handle continuous shooting with left mouse button held down
            if (Input.GetMouseButton(0) && !isReloading)
            {
                if (currentAmmoPerWeapon[currentWeaponIndex] > 0 && Time.time >= nextFireTime)
                {
                    Fire();
                    nextFireTime = Time.time + fireRate;
                }
                else if (currentAmmoPerWeapon[currentWeaponIndex] <= 0)
                {
                    StartCoroutine(Reload());
                }
                // Set IsGunAttacking on TPAnimator while button is held
                if (TPAnimator != null)
                {
                    TPAnimator.SetBool("IsGunAttacking", true);
                }
            }
            else
            {
                // Set IsGunAttacking to false when button is released
                if (TPAnimator != null)
                {
                    TPAnimator.SetBool("IsGunAttacking", false);
                }
            }

            // Handle reloading with R key
            if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoPerWeapon[currentWeaponIndex] < maxAmmoPerWeapon[currentWeaponIndex])
            {
                StartCoroutine(Reload());
            }
        }
    }

    void Fire()
    {
        if (!photonView.IsMine) return;
        if (currentAmmoPerWeapon[currentWeaponIndex] <= 0) return;

        // Decrease ammo
        currentAmmoPerWeapon[currentWeaponIndex]--;
        
        // Update UI immediately after decreasing ammo
        UpdateAmmoUI();

        // Play gunshot sound for everyone
        photonView.RPC(nameof(PlayGunshotSound), RpcTarget.All);

        RaycastHit _hit;
        Ray ray = FPS_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if(Physics.Raycast(ray, out _hit, 100))
        {
            photonView.RPC("CreateHitEffect", RpcTarget.All, _hit.point);

                    // Damage Enemy
        
            // Try to damage Guard (but guards are invulnerable)
            if (_hit.collider.gameObject.CompareTag("Guard"))
            {
                GuardHealth guardHealth = _hit.collider.gameObject.GetComponent<GuardHealth>();
                if (guardHealth != null)
                {
                    // Guards are invulnerable, but we still call TakeDamage to show the blocked effect
                    guardHealth.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, damage);
                    
                    // Optionally show a "blocked" message to the player
                    Debug.Log($"<color=yellow>Player:</color> Your attack was blocked by the invulnerable guard!");
                }
            }
        }

        // Sync ammo across network
        photonView.RPC("SyncAmmo", RpcTarget.All, currentWeaponIndex, currentAmmoPerWeapon[currentWeaponIndex]);

        if (photonView.IsMine)
        {
            UpdateAmmoUI();
        }
    }

    void SwordAttack()
    {
        if (!photonView.IsMine) return;

        StartCoroutine(PerformSwordAttack());
    }

    IEnumerator PerformSwordAttack()
    {
        if (FPAnimator != null) FPAnimator.SetBool("IsAttacking", true);
        if (TPAnimator != null) TPAnimator.SetBool("IsSwordAttacking", true);

        RaycastHit _hit;
        Ray ray = FPS_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if(Physics.Raycast(ray, out _hit, swordAttackRange))
        {
            photonView.RPC("CreateHitEffect", RpcTarget.All, _hit.point);

                    // Damage Enemy with sword
        
        // Try to damage Guard with sword (but guards are invulnerable)
        if (_hit.collider.gameObject.CompareTag("Guard"))
        {
            GuardHealth guardHealth = _hit.collider.gameObject.GetComponent<GuardHealth>();
            if (guardHealth != null)
            {
                // Guards are invulnerable, but we still call TakeDamage to show the blocked effect
                guardHealth.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, swordDamage);
                
                // Optionally show a "blocked" message to the player
                Debug.Log($"<color=yellow>Player:</color> Your sword attack was blocked by the invulnerable guard!");
            }
        }
        }
        
        yield return new WaitForSeconds(swordAttackRate * 0.9f); 

        if (FPAnimator != null) FPAnimator.SetBool("IsAttacking", false);
        if (TPAnimator != null) TPAnimator.SetBool("IsSwordAttacking", false);
    }

    [PunRPC]
    void SyncAmmo(int weaponIndex, int newAmmo)
    {
        currentAmmoPerWeapon[weaponIndex] = newAmmo;
        if (photonView.IsMine)
        {
            UpdateAmmoUI();
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        if (FPAnimator != null)
            FPAnimator.SetBool("IsReloading", true);

        // De-activate sword if it's active
        if (isSwordActive)
        {
            if (swordActivationCoroutine != null)
            {
                StopCoroutine(swordActivationCoroutine);
                swordActivationCoroutine = null;
            }
            // We call SyncSwordState directly instead of through an RPC
            // if we are sure this is only ever called on the local client.
            // Using an RPC is safer for maintaining state consistency.
            photonView.RPC("SyncSwordState", RpcTarget.All, false);
        }

        Debug.Log("Reloading...");
        yield return new WaitForSeconds(reloadTime);

        currentAmmoPerWeapon[currentWeaponIndex] = maxAmmoPerWeapon[currentWeaponIndex];
        UpdateAmmoUI();
        isReloading = false;

        if (FPAnimator != null)
            FPAnimator.SetBool("IsReloading", false);
        
        reloadCoroutine = null;
    }

    void UpdateAmmoUI()
    {
        // Only update UI for local player
        if (!photonView.IsMine) return;

        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmoPerWeapon[currentWeaponIndex]} / {maxAmmoPerWeapon[currentWeaponIndex]}";
        }
    }

    [PunRPC]
    public void CreateHitEffect(Vector3 position)
    {
        GameObject hitEffectGameobject = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        Destroy(hitEffectGameobject, 0.5f);
    }

    void SelectWeapon(int index)
    {
        for (int i = 0; i < weaponsFP.Length; i++)
        {
            if (weaponsFP[i] != null)
                weaponsFP[i].SetActive(i == index);
            if (weaponsTP[i] != null)
                weaponsTP[i].SetActive(i == index);
        }

        if (photonView.IsMine)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["WeaponIndex"] = index;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == photonView.Owner && changedProps.ContainsKey("WeaponIndex"))
        {
            int newIndex = (int)changedProps["WeaponIndex"];
            currentWeaponIndex = newIndex;
            ApplyWeaponVisual(currentWeaponIndex);
        }
    }

    // Modify SetWeaponIndex to handle the animation
    void SetWeaponIndex(int index)
    {
        if (isChanging)
        {
            Debug.Log($"Cannot switch weapon - already changing weapons");
            return;
        }

        if (isSwordActive)
        {
            if (swordActivationCoroutine != null)
            {
                StopCoroutine(swordActivationCoroutine);
                swordActivationCoroutine = null;
            }
            photonView.RPC("SyncSwordState", RpcTarget.All, false);
        }

        Debug.Log($"Starting weapon switch to index: {index}");
        currentWeaponIndex = index;
        StartCoroutine(SwitchWeaponAnimation(index));

        if (photonView.IsMine)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["WeaponIndex"] = index;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    // Add this new coroutine
    IEnumerator SwitchWeaponAnimation(int index)
    {
        try
        {
            Debug.Log("Starting weapon switch animation");
            isChanging = true;
            if (FPAnimator != null)
            {
                FPAnimator.SetBool("IsChanging", true);
            }

            yield return new WaitForSeconds(weaponSwitchTime);

            Debug.Log("Applying weapon visual changes");
            ApplyWeaponVisual(index);
        }
        finally
        {
            // Ensure these are always set back to false, even if an error occurs
            Debug.Log("Finishing weapon switch animation");
            if (FPAnimator != null)
            {
                FPAnimator.SetBool("IsChanging", false);
            }
            isChanging = false;
        }
    }

    // Modify ApplyWeaponVisual to update weapon stats but keep ammo separate
    void ApplyWeaponVisual(int index)
    {
        // Always update visuals for the local player after switching
        for (int i = 0; i < weaponsFP.Length; i++)
        {
            if (weaponsFP[i] != null)
                weaponsFP[i].SetActive(i == index);
            if (weaponsTP[i] != null)
                weaponsTP[i].SetActive(i == index);
        }

        // Update weapon data
        if (weaponsFP[index] != null)
        {
            var weaponData = weaponsFP[index].GetComponent<Weapon>();
            if (weaponData != null)
            {
                fireRate = weaponData.fireRate;
                damage = weaponData.damage;
                // Don't update maxAmmo here anymore since we track it per weapon
                UpdateAmmoUI();
                Debug.Log($"Switched weapon: fireRate={fireRate}, damage={damage}, currentAmmo={currentAmmoPerWeapon[index]}/{maxAmmoPerWeapon[index]}");
            }
        }
    }

    public void ActivateSword(float duration)
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            isReloading = false;
            if (FPAnimator != null) FPAnimator.SetBool("IsReloading", false);
            reloadCoroutine = null;
        }

        if (swordActivationCoroutine != null)
        {
            StopCoroutine(swordActivationCoroutine);
        }
        swordActivationCoroutine = StartCoroutine(SwordActivationCoroutine(duration));
    }

    IEnumerator SwordActivationCoroutine(float duration)
    {
        photonView.RPC("SyncSwordState", RpcTarget.All, true);
        yield return new WaitForSeconds(duration);
        photonView.RPC("SyncSwordState", RpcTarget.All, false);
        swordActivationCoroutine = null;
    }

    [PunRPC]
    void SyncSwordState(bool swordState)
    {
        isSwordActive = swordState;

        if (Sword_FP != null) Sword_FP.SetActive(isSwordActive);
        if (Sword_TP != null) Sword_TP.SetActive(isSwordActive);

        if (FPAnimator != null) FPAnimator.SetBool("IsSword", isSwordActive);

        if (isSwordActive)
        {
            for (int i = 0; i < weaponsFP.Length; i++)
            {
                if (weaponsFP[i] != null) weaponsFP[i].SetActive(false);
                if (weaponsTP[i] != null) weaponsTP[i].SetActive(false);
            }
        }
        else
        {
            ApplyWeaponVisual(currentWeaponIndex);
        }
    }

    [PunRPC]
    void PlayGunshotSound()
    {
        if (PlayerAudio.Instance != null)
        {
            PlayerAudio.Instance.PlaySFX("Gun Shot");
        }
    }
}
