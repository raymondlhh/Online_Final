using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpaceShipManager : MonoBehaviourPunCallbacks
{
    public Transform[] seats; // Assign Seat1, Seat2, Seat3, Seat4 in inspector
    private Dictionary<int, int> playerToSeatMap = new Dictionary<int, int>(); // ViewID to Seat index

    public static SpaceShipManager Instance;

    public GameObject onBoardUI;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetAvailableSeatIndex()
    {
        for (int i = 0; i < seats.Length; i++)
        {
            if (!playerToSeatMap.ContainsValue(i))
            {
                return i;
            }
        }
        return -1; // No seat available
    }

    public bool IsSeatTaken(int index)
    {
        return playerToSeatMap.ContainsValue(index);
    }

    public void AssignPlayerToSeat(int playerViewID)
    {
        

        if (playerToSeatMap.ContainsKey(playerViewID)) return;

        int seatIndex = -1;

        // Try seat 0 (Seat1) first
        if (!IsSeatTaken(0))
            seatIndex = 0;
        else
        {
            // Try random available seat among Seat2-4
            List<int> availableSeats = new List<int>();
            for (int i = 1; i < seats.Length; i++)
            {
                if (!IsSeatTaken(i)) availableSeats.Add(i);
            }

            if (availableSeats.Count > 0)
                seatIndex = availableSeats[Random.Range(0, availableSeats.Count)];
        }

        if (seatIndex != -1)
        {
            photonView.RPC("RPC_AssignSeat", RpcTarget.All, playerViewID, seatIndex);
        }
    }

    [PunRPC]
    void RPC_AssignSeat(int viewID, int seatIndex)
    {
        GameObject playerObj = PhotonView.Find(viewID)?.gameObject;
        if (playerObj != null)
        {
            playerToSeatMap[viewID] = seatIndex;
            playerObj.transform.position = seats[seatIndex].position;
            playerObj.transform.rotation = seats[seatIndex].rotation;

            playerObj.transform.SetParent(seats[seatIndex]);

            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.CanMove = false;

                Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.velocity = Vector3.zero;
                }

                CapsuleCollider col = playerObj.GetComponent<CapsuleCollider>();
                if (col != null) col.enabled = false;
            }

            if (playerObj.GetComponent<PhotonView>().IsMine)
            {
                if (onBoardUI != null)
                    onBoardUI.SetActive(false);

                if (seatIndex == 0)
                {
                    StartSpaceShip.Instance.ShowSeat1ProgressBar();
                    playerObj.GetComponent<PlayerInteraction>().SetStartTriggerState(true);
                }
            }
        }
    }

    public void ResetSeats()
    {
        playerToSeatMap.Clear();
    }
}
