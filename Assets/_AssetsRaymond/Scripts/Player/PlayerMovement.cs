using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.SceneManagement;

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
    private Rigidbody rb;
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

    [Header("Animation Settings")]
    public Animator FPAnimator;  // First Person Animator
    public Animator TPAnimator;  // Third Person Animator
    private PhotonView photonView;

    // Jump improvement variables
    private bool jumpRequested = false;
    private float jumpCooldown = 0.1f;
    private float lastJumpTime = -1f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;
        photonView = GetComponent<PhotonView>();

        // Unlock and show cursor if in ChooseCharacterScene
        if (SceneManager.GetActiveScene().name == "ChooseCharacterScene")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CanMove = false;
            CanLook = false;
        }
        else
        {
            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView != null && !photonView.IsMine)
            return;

        // Ground Check - moved to FixedUpdate for better physics sync
        if (groundCheck != null)
        {
            // Casts a ray straight down from the groundCheck position.
            isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance);
        }

        // The logic for toggling cursor lock with Escape has been moved to the PauseManager
        // to prevent input conflicts.

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

            // Handle running with shift key
            isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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
}
