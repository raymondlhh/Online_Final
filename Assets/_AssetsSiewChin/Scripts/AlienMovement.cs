using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AlienMovement : MonoBehaviourPunCallbacks
{
    public Transform player;               // Target player (assigned at runtime)
    public float floatTargetY = 10f;
    public float floatStartY = 20f;
    public float floatDownSpeed = 2f;
    public float moveSpeed = 3f;
    public float stopDistance = 1f;

    private bool hasDescended = false;


    void Start()
    {
        
        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, floatStartY, pos.z);

        StartCoroutine(FloatDownToY());

        // Find the first player to follow (can be improved to follow closest, etc.)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            player = players[0].transform;
        }
    }

    IEnumerator FloatDownToY()
    {
        while (transform.position.y > floatTargetY + 0.1f)
        {
            Vector3 pos = transform.position;
            float newY = Mathf.MoveTowards(pos.y, floatTargetY, floatDownSpeed * Time.deltaTime);
            transform.position = new Vector3(pos.x, newY, pos.z);
            yield return null;
        }

        transform.position = new Vector3(transform.position.x, floatTargetY, transform.position.z);
        hasDescended = true;
    }

    void Update()
    {
        // Only Master Client controls movement
        if (!PhotonNetwork.IsMasterClient || !hasDescended || player == null) return;

        Transform closestPlayer = GetClosestPlayer();
        if (closestPlayer == null) return;

        Vector3 alienXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerXZ = new Vector3(closestPlayer.position.x, 0, closestPlayer.position.z);
        float distance = Vector3.Distance(alienXZ, playerXZ);


        if (distance > stopDistance)
        {
            Vector3 targetPos = new Vector3(closestPlayer.position.x, floatTargetY, closestPlayer.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            // Face the player (optional)
            Vector3 directionToPlayer = closestPlayer.position - transform.position;
if (directionToPlayer != Vector3.zero)
{
    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
}
        }
    }

    Transform GetClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = player.transform;
            }
        }

        return closest;
    }
}
