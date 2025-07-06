using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Hint1BManager : MonoBehaviour
{
    public static Hint1BManager Instance;

    [Header("UI Elements")]
    public GameObject hint1BUI;

    private HashSet<int> playersInArea = new HashSet<int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        hint1BUI.SetActive(false);
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
        hint1BUI.SetActive(visible);
    }

}
