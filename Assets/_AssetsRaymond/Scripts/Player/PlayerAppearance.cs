using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerAppearance : MonoBehaviourPun
{
    [Header("Body Mesh & Decorations")]
    public SkinnedMeshRenderer bodyRenderer;
    public Mesh JadenMesh;
    public Mesh AliceMesh;
    public Mesh JackMesh;
    public GameObject JadenDecoration;
    public GameObject AliceDecoration;
    public GameObject JackDecoration;

    // call this from your UIManager
    public void SetJaden()  
    { 
        if (photonView.IsMine) 
        {
            Debug.Log("PlayerAppearanceController: Setting Jaden appearance via RPC");
            photonView.RPC(nameof(RPC_ChangeAppearance), RpcTarget.AllBuffered, 0); 
        }
        else
        {
            Debug.LogWarning("PlayerAppearanceController: Cannot set appearance - not mine!");
        }
    }
    
    public void SetAlice()  
    { 
        if (photonView.IsMine) 
        {
            Debug.Log("PlayerAppearanceController: Setting Alice appearance via RPC");
            photonView.RPC(nameof(RPC_ChangeAppearance), RpcTarget.AllBuffered, 1); 
        }
        else
        {
            Debug.LogWarning("PlayerAppearanceController: Cannot set appearance - not mine!");
        }
    }
    
    public void SetJack()   
    { 
        if (photonView.IsMine) 
        {
            Debug.Log("PlayerAppearanceController: Setting Jack appearance via RPC");
            photonView.RPC(nameof(RPC_ChangeAppearance), RpcTarget.AllBuffered, 2); 
        }
        else
        {
            Debug.LogWarning("PlayerAppearanceController: Cannot set appearance - not mine!");
        }
    }

    [PunRPC]
    void RPC_ChangeAppearance(int skinIndex)
    {
        Debug.Log($"PlayerAppearanceController: RPC_ChangeAppearance called with skinIndex: {skinIndex} on {gameObject.name}");
        
        // pick the right mesh+decoration
        switch (skinIndex)
        {
            case 0: 
                ChangeAppearance(JadenMesh, JadenDecoration); 
                //Debug.Log("PlayerAppearanceController: Applied Jaden appearance");
                break;
            case 1: 
                ChangeAppearance(AliceMesh, AliceDecoration); 
                //Debug.Log("PlayerAppearanceController: Applied Alice appearance");
                break;
            case 2: 
                ChangeAppearance(JackMesh, JackDecoration);  
                //Debug.Log("PlayerAppearanceController: Applied Jack appearance");
                break;
            default:
                Debug.LogError($"PlayerAppearanceController: Invalid skinIndex: {skinIndex}");
                break;
        }
    }

    void ChangeAppearance(Mesh newMesh, GameObject activeDecoration)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.sharedMesh = newMesh;
            //Debug.Log($"PlayerAppearanceController: Changed mesh to {newMesh.name}");
        }
        else
        {
            Debug.LogError("PlayerAppearanceController: bodyRenderer is null!");
        }

        // turn off all, then on the one we want
        if (JadenDecoration != null) JadenDecoration.SetActive(false);
        if (AliceDecoration != null) AliceDecoration.SetActive(false);
        if (JackDecoration != null) JackDecoration.SetActive(false);
        
        if (activeDecoration != null) 
        {
            activeDecoration.SetActive(true);
            //Debug.Log($"PlayerAppearanceController: Activated decoration {activeDecoration.name}");
        }
        else
        {
            Debug.LogWarning("PlayerAppearanceController: activeDecoration is null!");
        }
    }

    void Start()
    {
        // // Verify that we have a PhotonView
        // if (photonView == null)
        // {
        //     Debug.LogError("PlayerAppearanceController: No PhotonView found on this GameObject!");
        // }
        // else
        // {
        //     Debug.Log($"PlayerAppearanceController: PhotonView found. IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
        // }

        // // Verify that we have the required components
        // if (bodyRenderer == null)
        // {
        //     Debug.LogError("PlayerAppearanceController: bodyRenderer is not assigned!");
        // }

        // if (JadenMesh == null || AliceMesh == null || JackMesh == null)
        // {
        //     Debug.LogError("PlayerAppearanceController: One or more meshes are not assigned!");
        // }

        // if (JadenDecoration == null || AliceDecoration == null || JackDecoration == null)
        // {
        //     Debug.LogError("PlayerAppearanceController: One or more decorations are not assigned!");
        // }
    }
}
