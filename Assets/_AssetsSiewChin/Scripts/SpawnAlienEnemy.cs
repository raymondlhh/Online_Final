using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnAlienEnemy : MonoBehaviour
{
    public GameObject AlienEnemy1;
    public GameObject AlienEnemy2;
    public GameObject AlienEnemy3;
    
    // Start is called before the first frame update
    void Start()
    {
        AlienEnemy1.SetActive(false);
        AlienEnemy2.SetActive(false);
        AlienEnemy3.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            if(CrystalManager.Instance.AllCollected)
            {
                AlienEnemy1.SetActive(true);
                AlienEnemy2.SetActive(true);
                AlienEnemy3.SetActive(true);
            }
        }
    }
}
