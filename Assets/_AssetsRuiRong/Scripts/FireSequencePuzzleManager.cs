using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FireSequencePuzzleManager : MonoBehaviourPunCallbacks
{
    public static FireSequencePuzzleManager Instance;

    public List<FireSequencePuzzle> fireTorches; // Assign in correct order
    private int currentIndex = 0;
    private bool puzzleSolved = false;

    [Header("On Solve")]
    public Animator doorAnimator;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (doorAnimator != null)
        {
            doorAnimator.enabled = false; // Disable animator at the start
        }
    }

    public void AttemptIgnite(int torchIndex)
    {
        if (puzzleSolved) return;

        if (torchIndex == currentIndex)
        {
            fireTorches[torchIndex].Ignite();
            currentIndex++;

            if (currentIndex >= fireTorches.Count)
            {
                puzzleSolved = true;
                photonView.RPC("RPC_PuzzleSolved", RpcTarget.All);
            }
        }
        else
        {
            Debug.Log("Wrong fire lit. Resetting...");
            ResetAllFires();
        }
    }

    void ResetAllFires()
    {
        currentIndex = 0;

        foreach (FireSequencePuzzle torch in fireTorches)
        {
            torch.Extinguish();
        }
    }

    [PunRPC]
    void RPC_PuzzleSolved()
    {
        Debug.Log(" Puzzle Solved!");

        if (doorAnimator != null)
        {
            doorAnimator.enabled = true; // Could also trigger door animator here
        }
    }
}
