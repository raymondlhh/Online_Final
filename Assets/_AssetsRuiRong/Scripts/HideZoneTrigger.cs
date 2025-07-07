using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HideZoneTrigger : MonoBehaviour
{
    public Transform HideSpot;
    
    // Start is called before the first frame update
    void Start()
    {
        
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
                PlayerInteraction interaction = other.GetComponent<PlayerInteraction>();
                interaction.SetHideTriggerState(true);
                
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
                PlayerInteraction interaction = other.GetComponent<PlayerInteraction>();
                interaction.SetHideTriggerState(false);
                
            }
        }
    }
}
