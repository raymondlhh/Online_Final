using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlienForTrailer : MonoBehaviour
{
    public GameObject alien;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Alien(2f));
        alien.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Alien(float delay)
    {
        yield return new WaitForSeconds(delay);
        alien.SetActive(true);
    }


}
