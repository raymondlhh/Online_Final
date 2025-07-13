using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SpinPuzzle : MonoBehaviourPunCallbacks
{
    private Material originalMaterial;
    public Material highlightMaterial;
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
    }

    public void SetHighlight(bool highlight)
    {
        if (meshRenderer == null || highlightMaterial == null) return;

        meshRenderer.material = highlight ? highlightMaterial : originalMaterial;
    }

    public void TryRotate()
    {
        photonView.RPC("RPC_Rotate", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_Rotate()
    {
        transform.Rotate(0, 10f, 0);
    }
}
