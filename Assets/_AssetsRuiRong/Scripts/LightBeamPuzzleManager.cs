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
        CatLightBeam.instance.StartScaling();
        StartCoroutine(OpenDoorAfterDelay(3f));

    }

    private IEnumerator OpenDoorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (doorAnimator != null)
        {
            doorAnimator.enabled = true;
        }
    }
}
