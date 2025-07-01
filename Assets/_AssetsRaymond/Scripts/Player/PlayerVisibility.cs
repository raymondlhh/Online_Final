using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerVisibility : MonoBehaviourPunCallbacks
{
    public GameObject fpViewObject;
    public GameObject fpPlayerUI;
    public GameObject tpViewObject;
    public GameObject tpPlayerUI;
    private Coroutine visibilityCoroutine;
    private int playerLayer;
    private int cloakedPlayerLayer;

    void Awake()
    {
        // Get the integer representation of the layers
        playerLayer = LayerMask.NameToLayer("Player");
        cloakedPlayerLayer = LayerMask.NameToLayer("CloakedPlayer");
    }

    public void SetFirstPersonVisibility(bool isVisible)
    {
        if (fpViewObject != null)
        {
            fpViewObject.SetActive(isVisible);
        }
        if (fpPlayerUI != null)
        {
            fpPlayerUI.SetActive(isVisible);
        }
    }

    public void SetThirdPersonVisibility(bool isVisible)
    {
        if (tpViewObject != null)
        {
            tpViewObject.SetActive(isVisible);
        }
        if (tpPlayerUI != null)
        {
            tpPlayerUI.SetActive(isVisible);
        }
    }

    public void SetPlayerVisible(bool isVisible)
    {
        SetThirdPersonVisibility(isVisible);
    }

    public void ActivateGhostCloak(float duration)
    {
        if (visibilityCoroutine != null) StopCoroutine(visibilityCoroutine);
        visibilityCoroutine = StartCoroutine(GhostCloakCoroutine(duration));
    }

    [PunRPC]
    private void TeleportRPC(Vector3 position)
    {
        transform.position = position;
    }

    public void Teleport(Vector3 position)
    {
        // Use an RPC to ensure all clients see the teleportation.
        photonView.RPC("TeleportRPC", RpcTarget.All, position);
    }

    private IEnumerator GhostCloakCoroutine(float duration)
    {
        photonView.RPC("SyncVisibility", RpcTarget.All, false, true); // Cloak starts
        yield return new WaitForSeconds(duration);
        photonView.RPC("SyncVisibility", RpcTarget.All, true, false); // Cloak ends
        visibilityCoroutine = null;
    }

    [PunRPC]
    private void SyncVisibility(bool isVisible, bool isCloaked)
    {
        SetPlayerVisible(isVisible);
        
        // Change the layer for the local player and all remote representations.
        if (isCloaked)
        {
            SetLayerRecursively(gameObject, cloakedPlayerLayer);
        }
        else
        {
            SetLayerRecursively(gameObject, playerLayer);
        }
    }

    // Helper function to apply a layer to a GameObject and all its children.
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        
        // Don't change the layer of the VisualEffects object or its children.
        if (obj.name == "VisualEffects")
        {
            return;
        }

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
} 