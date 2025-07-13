using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SpinTriggerZone : MonoBehaviour
{
    public GameObject AimPoint;
    // Start is called before the first frame update
    void Start()
    {
        AimPoint.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                AimPoint.SetActive(true);
                other.GetComponent<PlayerInteraction>().SetSpinTriggerState(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                AimPoint.SetActive(false);
                other.GetComponent<PlayerInteraction>().SetSpinTriggerState(false);
            }
        }
    }
}
