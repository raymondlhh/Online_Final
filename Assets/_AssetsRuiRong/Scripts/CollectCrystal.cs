using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Only MasterClient should update the count
            if (Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                CrystalManager.Instance.CollectCrystal();
            }

            Photon.Pun.PhotonNetwork.Destroy(gameObject); // Destroy crystal for all players
        }
    }
}
