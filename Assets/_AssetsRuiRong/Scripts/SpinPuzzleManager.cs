using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SpinPuzzleManager : MonoBehaviourPunCallbacks
{
    [Header("GateAnimator")]
    public Animator doorAnimator;

    [Header("Spins")]
    public SpinPuzzle SpinA;
    public SpinPuzzle SpinB;
    public SpinPuzzle SpinC;
    public SpinPuzzle SpinD;
    public SpinPuzzle SpinE;

    private bool puzzleSolved = false;

    // Start is called before the first frame update
    void Start()
    {
        if (doorAnimator != null)
        {
            doorAnimator.enabled = false; // Disable animator at the start
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (puzzleSolved) return;

        float aY = SpinA.transform.eulerAngles.y % 360f;
        float bY = SpinB.transform.eulerAngles.y % 360f;
        float cY = SpinC.transform.eulerAngles.y % 360f;
        float dY = SpinD.transform.eulerAngles.y % 360f;
        float eY = SpinE.transform.eulerAngles.y % 360f;

        float tolerance = 0.01f; // Adjust as needed

        if (Mathf.Abs(aY - 0f) < tolerance &&
            Mathf.Abs(bY - 0f) < tolerance &&
            Mathf.Abs(cY - 0f) < tolerance &&
            Mathf.Abs(dY - 0f) < tolerance &&
            Mathf.Abs(eY - 0f) < tolerance)
        {
            Debug.Log("All rotations correct. Opening gate!");
            puzzleSolved = true;
            ActivateGate();
        }
    }

    public void ActivateGate()
    {
        photonView.RPC("RPC_Activategate", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_Activategate()
    {
        doorAnimator.enabled = true;
    }
}
