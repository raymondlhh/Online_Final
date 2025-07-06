using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Hint1AManager : MonoBehaviour
{
    public static Hint1AManager Instance;

    [Header("UI Elements")]
    public GameObject hint1AUI;

    private HashSet<int> playersInArea = new HashSet<int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        hint1AUI.SetActive(false);
    }

    void Update()
    {

    }

    // Called when the player enters the door area
    public void AddPlayer(int viewID)
    {
        if (!playersInArea.Contains(viewID))
        {
            playersInArea.Add(viewID);
        }

        if (PhotonView.Find(viewID).IsMine)
        {
            SetLocalUIVisibility(true);
        }
    }

    // Called when the player exits the door area
    public void RemovePlayer(int viewID)
    {
        if (playersInArea.Contains(viewID))
        {
            playersInArea.Remove(viewID);
        }

        if (PhotonView.Find(viewID).IsMine)
        {
            SetLocalUIVisibility(false);
        }
    }

    // Called to show/hide the UI on local player's screen
    public void SetLocalUIVisibility(bool visible)
    {
        hint1AUI.SetActive(visible);
    }

}
