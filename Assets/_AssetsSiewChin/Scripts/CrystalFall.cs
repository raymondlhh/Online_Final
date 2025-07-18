using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CrystalFall : MonoBehaviour
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
        if (other.CompareTag("Light"))
        {
            Destroy(gameObject);
        }
    }
}
