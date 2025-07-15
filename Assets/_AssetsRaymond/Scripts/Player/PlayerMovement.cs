using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public bool CanMove { get; set; } = true;
    public bool CanLook { get; set; } = true;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float jumpForce = 5f;
    private float currentSpeed;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    public Rigidbody rb;
    private bool isRunning = false;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    private bool isGrounded;
    private bool canMultiJump = false;
    private bool isSlowFalling = false;
    private Coroutine slowFallCoroutine;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;
    private float verticalRotation = 0f;
    public Camera playerCamera;
    private bool isCursorLocked = true;
    private bool isCursorToggled = false;

    [Header("Animation Settings")]
    public Animator FPAnimator;  // First Person Animator
    public Animator TPAnimator;  // Third Person Animator
    private PhotonView photonView;

    // Jump improvement variables
    private bool jumpRequested = false;
    private float jumpCooldown = 0.1f;
    private float lastJumpTime = -1f;

    [Header("Stamina Settings")]
    public Image StaminaBar; // Assign in inspector
    public float maxStamina = 5f; // seconds of running
    public float staminaDrainRate = 1f; // per second
    public float staminaRegenRate = 0.5f; // per second
    private float currentStamina;
    private bool isStaminaDepleted = false;
    [HideInInspector]
    public bool isSprintBoostActive = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;
        photonView = GetComponent<PhotonView>();

        // Only the local player controls the cursor
        if (photonView != null && photonView.IsMine)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == "ChooseCharacterScene" || sceneName == "ChooseCharacterManager")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                CanMove = false;
                CanLook = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // If no camera is assigned, try to find it
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("No camera found! Please assign a camera to the player.");
            }
        }

        // Initialize stamina
        currentStamina = maxStamina;
        if (StaminaBar != null)
            StaminaBar.fillAmount = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView != null && !photonView.IsMine)
            return;

        if (photonView != null && photonView.IsMine)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == "ChooseCharacterScene" || sceneName == "ChooseCharacterManager")
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    isCursorToggled = !isCursorToggled;
                }
                if (isCursorToggled)
                {
                    if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
                else
                {
                    if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
            }
        }

        // Ground Check - moved to FixedUpdate for better physics sync
        if (groundCheck != null)
        {
            // Casts a ray straight down from the groundCheck position.
            isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance);
        }

        if (isCursorLocked && CanLook)
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Rotate the player (horizontal rotation)
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera (vertical rotation)
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        if (CanMove)
        {
            // Get movement input
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");

            // Handle running with shift key, only if stamina is not depleted and not sprint boost
            bool wantsToRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            isRunning = wantsToRun && !isStaminaDepleted && !isSprintBoostActive && (horizontalInput != 0 || verticalInput != 0);
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Calculate movement direction
            moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
            moveDirection = moveDirection.normalized;

            // Update both FP and TP animators
            UpdateAnimators();

            // Handle jumping input - moved to FixedUpdate for physics sync
            if (Input.GetKeyDown(KeyCode.Space) && Time.time > lastJumpTime + jumpCooldown)
            {
                jumpRequested = true;
            }
        }
        else
        {
            // Reset inputs when movement is disabled
            horizontalInput = 0f;
            verticalInput = 0f;
            moveDirection = Vector3.zero;
            jumpRequested = false;
        }

        // Handle stamina drain and regen
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !isSprintBoostActive && (horizontalInput != 0 || verticalInput != 0))
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isStaminaDepleted = true;
            }
        }
        else
        {
            // Regenerate stamina when not running
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina)
                    currentStamina = maxStamina;
            }
            if (currentStamina > 0.1f)
                isStaminaDepleted = false;
        }
        // Update the UI
        if (StaminaBar != null)
            StaminaBar.fillAmount = currentStamina / maxStamina;
    }

    // FixedUpdate is called at a fixed time interval and is independent of frame rate
    void FixedUpdate()
    {
        if (photonView != null && !photonView.IsMine)
            return;

        // Improved ground check using CheckSphere for better reliability
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, LayerMask.GetMask("Default"));
        }

        if (CanMove)
        {
            // Apply movement - only set X and Z velocity to avoid interfering with jump
            Vector3 movement = moveDirection * currentSpeed;
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);

            // Handle jumping in FixedUpdate for better physics sync
            if (jumpRequested && (isGrounded || canMultiJump))
            {
                // Reset Y velocity before applying jump force for consistent jump height
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                lastJumpTime = Time.time;
                jumpRequested = false;
            }
        }
        else
        {
            // Ensure no horizontal movement is applied when movement is disabled
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }

        if (isSlowFalling)
        {
            // Override y-velocity to counteract gravity
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }
    }

    private void UpdateAnimators()
    {
        // Update First Person Animator
        if (FPAnimator != null)
        {
            // No longer setting IsRunning for FPAnimator
        }

        // Update Third Person Animator
        if (TPAnimator != null)
        {
            TPAnimator.SetFloat("Horizontal", horizontalInput);
            TPAnimator.SetFloat("Vertical", verticalInput);
            TPAnimator.SetBool("IsRunning", isRunning);
        }
    }

    public void ActivateHighJump(float duration)
    {
        StartCoroutine(HighJumpCoroutine(duration));
    }

    private IEnumerator HighJumpCoroutine(float duration)
    {
        canMultiJump = true;
        yield return new WaitForSeconds(duration);
        canMultiJump = false;
    }

    public void ActivateSlowFall(float duration)
    {
        if (slowFallCoroutine != null) StopCoroutine(slowFallCoroutine);
        slowFallCoroutine = StartCoroutine(SlowFallCoroutine(duration));
    }

    public void DeactivateSlowFall()
    {
        if (slowFallCoroutine != null)
        {
            StopCoroutine(slowFallCoroutine);
            slowFallCoroutine = null;
        }
        isSlowFalling = false;
        rb.useGravity = true; // Re-enable gravity
    }

    private IEnumerator SlowFallCoroutine(float duration)
    {
        isSlowFalling = true;
        rb.useGravity = false;
        yield return new WaitForSeconds(duration);
        DeactivateSlowFall();
    }

    [Photon.Pun.PunRPC]
    public void SetKinematicState(bool state)
    {
        if (rb != null)
        {
            rb.isKinematic = state;
        }
    }

    [PunRPC]
    public void RPC_Float(float duration)
    {
        StartCoroutine(FloatEffect(duration));
    }

    IEnumerator FloatEffect(float duration)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = false;
            rb.velocity = new Vector3(rb.velocity.x, 2f, rb.velocity.z);
            yield return new WaitForSeconds(duration);
            rb.useGravity = true;
        }
    }
}
