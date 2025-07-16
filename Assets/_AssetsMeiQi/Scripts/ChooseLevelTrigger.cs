using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ChooseLevelTrigger : MonoBehaviour
{
    
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
        Debug.Log("Trigger!");
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                SubLevelInteraction interaction = other.GetComponent<SubLevelInteraction>();
                interaction.SetChooseLevelUI(true);
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
                SubLevelInteraction interaction = other.GetComponent<SubLevelInteraction>();
                interaction.SetChooseLevelUI(false);
            }
        }
    }
}
