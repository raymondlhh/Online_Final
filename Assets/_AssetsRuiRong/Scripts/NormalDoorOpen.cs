using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class NormalDoorOpen : MonoBehaviour
{
    public float openAngleY = 90f;
    public float openSpeed = 2f;

    private bool isPlayerNearby = false;
    private bool isOpening = false;

    private Quaternion targetRotation;
    private Quaternion initialRotation;

    private TextMeshProUGUI promptText;
    private Transform playerCamera;
    
    // Start is called before the first frame update
    void Start()
    {
        initialRotation = transform.rotation;

        targetRotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.y + openAngleY,
            transform.rotation.eulerAngles.z);
    }

    // Update is called once per frame
    void Update()
    {
        if(isOpening)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);

        }

        if (isPlayerNearby) // && when player press E
        {
            isOpening = true;

            if(promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            //PhotonView view = other.GetC
        }
    }
}
