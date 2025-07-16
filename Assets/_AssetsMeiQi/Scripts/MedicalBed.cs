using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MedicalBed : MonoBehaviourPun
{
    public Transform lieDownPoint;
    private bool isOccupied = false;

    public float moveDistance = 4f;
    public float moveSpeed = 2f;

    private Vector3 originalPosition;
    private Vector3 targetPosition;

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = originalPosition - new Vector3(0, 0, moveDistance);
    }

    [PunRPC]
    public void RPC_LieDown(int playerViewID)
    {
        if (isOccupied) return;

        Debug.Log("Running RPC_LieDown");

        PhotonView playerPV = PhotonView.Find(playerViewID);
        if (playerPV != null)
        {
            GameObject player = playerPV.gameObject;
            StartCoroutine(HandleLyingDown(player));
        }
    }

    IEnumerator HandleLyingDown(GameObject player)
    {
        isOccupied = true;

        // Save original position & rotation
        Vector3 originalPos = player.transform.position;
        Quaternion originalRot = player.transform.rotation;
        Transform originalParent = player.transform.parent;

        // Lock movement if PlayerMovement exists
        PlayerMovement moveScript = player.GetComponent<PlayerMovement>();
        if (moveScript != null)
        {
            moveScript.CanMove = false;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.enabled = false;
        }

        // Move player to bed
        player.transform.SetParent(transform);
        player.transform.position = lieDownPoint.position;
        player.transform.rotation = lieDownPoint.rotation;

        StartMoveSequence();
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.RegainHealth();

        yield return new WaitForSeconds(7f);

        // Return to original position
        player.transform.SetParent(originalParent);
        player.transform.position = originalPos;
        player.transform.rotation = originalRot;

        if (moveScript != null)
        {
            moveScript.CanMove = true;
        }

        if (rb != null)
        {
            rb.useGravity = true;
        }

        if (capsule != null)
        {
            capsule.enabled = true;
        }

        isOccupied = false;
    }

    public void StartMoveSequence()
    {
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        // Move forward
        yield return StartCoroutine(MoveToPosition(targetPosition));

        // Wait
        yield return new WaitForSeconds(1f);

        // Move back
        yield return StartCoroutine(MoveToPosition(originalPosition));
    }

    IEnumerator MoveToPosition(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = destination; // snap to exact position
    }
}
