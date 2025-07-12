using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LightBeamHint : MonoBehaviour
{
    public GameObject HintUI;
    
    // Start is called before the first frame update
    void Start()
    {
        HintUI.SetActive(false);
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
                HintUI.SetActive(true);
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
                HintUI.SetActive(false);
            }
        }
    }

}
