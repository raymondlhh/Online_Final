using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LightBeamPuzzleManager : MonoBehaviourPunCallbacks
{
    [Header("LightBeams")]
    public GameObject lightbeam1;
    public GameObject lightbeam2;

    [Header("DoorAnimator")]
    public Animator doorAnimator;

    public RotateStatue statueA;
    public RotateStatue statueB;

    private bool puzzleSolved = false;


    // Start is called before the first frame update
    void Start()
    {
        lightbeam1.SetActive(false);
        lightbeam2.SetActive(false);
        
        if (doorAnimator != null)
        {
            doorAnimator.enabled = false; // Disable animator at the start
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (puzzleSolved) return;

        float aY = statueA.transform.eulerAngles.y % 360f;
        float bY = statueB.transform.eulerAngles.y % 360f;

        if (Mathf.Approximately(aY, 270f) && Mathf.Approximately(bY, 90f))
        {
            puzzleSolved = true;
            ActivateLightBeam();
        }
    }

    public void ActivateLightBeam()
    {
        photonView.RPC("RPC_ActivatePuzzle", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_ActivatePuzzle()
    {
        lightbeam1.SetActive(true);
        lightbeam2.SetActive(true);
        //CatLightBeam.instance.StartScaling();
        StartCoroutine(OpenDoorAfterDelay(7f));

    }

    private IEnumerator OpenDoorAfterDelay(float delay)
    {
        Debug.Log("Will open the door!");
        yield return new WaitForSeconds(delay);

        if (doorAnimator != null)
        {
            doorAnimator.enabled = true;
        }
        Destroy(lightbeam1);
        Destroy(lightbeam2);
        Photon.Pun.PhotonNetwork.Destroy(lightbeam1);
        Photon.Pun.PhotonNetwork.Destroy(lightbeam2);
    }
}
