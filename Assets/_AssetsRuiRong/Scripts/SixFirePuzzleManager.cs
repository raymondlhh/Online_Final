using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SixFirePuzzleManager : MonoBehaviourPunCallbacks
{
    public static SixFirePuzzleManager Instance;

    private HashSet<FireTorch> litFires = new HashSet<FireTorch>();
    public int totalFires = 6;

    [Header("Gate Animation")]
    public Animator GateAnimator;
    private bool gateOpened = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (GateAnimator != null)
        {
            GateAnimator.enabled = false; // Disable animator at the start
        }
    }

    public void RegisterLitFire(FireTorch fire)
    {
        litFires.Add(fire);

        if (!gateOpened && litFires.Count == totalFires)
        {
            photonView.RPC("RPC_OpenGate", RpcTarget.All);
        }
    }

    public void UnregisterLitFire(FireTorch fire)
    {
        if (litFires.Contains(fire))
        {
            litFires.Remove(fire);
        }
    }

    public bool AllFiresLit()
    {
        return litFires.Count == totalFires;
    }

    [PunRPC]
    void RPC_OpenDoor()
    {
        if (gateOpened) return;

        gateOpened = true;
        GateAnimator.enabled = true;  // Set up a trigger in the Animator to play open animation
        Debug.Log("All fires lit! Door opened!");
    }

}
