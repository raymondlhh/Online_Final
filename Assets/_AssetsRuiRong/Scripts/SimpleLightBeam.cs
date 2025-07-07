using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLightBeam : MonoBehaviour
{
    public GameObject lightbeam1;
    public GameObject lightbeam2;
    
    // Start is called before the first frame update
    void Start()
    {
        lightbeam1.SetActive(false);
        lightbeam2.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            lightbeam1.SetActive(true);
            lightbeam2.SetActive(true);
        }
    }
}
