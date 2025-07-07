using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CollectCrystal : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Crystal!");
        
        if (other.CompareTag("Player"))
        {
            // Only MasterClient should update the count
            if (Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                CrystalManager.Instance.CollectCrystal();
            }
            Destroy(gameObject);
            Photon.Pun.PhotonNetwork.Destroy(gameObject); // Destroy crystal for all players
        }
    }
}
