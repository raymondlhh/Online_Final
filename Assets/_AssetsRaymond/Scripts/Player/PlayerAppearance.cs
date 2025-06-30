using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerAppearance : MonoBehaviourPunCallbacks
{
    [Header("Body Mesh & Materials")]
    public SkinnedMeshRenderer bodyRenderer;
    public Mesh maleMesh;
    public Mesh femaleMesh;
    public Material[] colorMaterials; // 8 materials, assign in Inspector

    [Header("Decorations")]
    public GameObject maleDecoration;
    public GameObject femaleDecoration;

    // Set gender (0 = male, 1 = female)
    public void SetGender(int genderIndex)
    {
        if (bodyRenderer != null)
            bodyRenderer.sharedMesh = (genderIndex == 0) ? maleMesh : femaleMesh;

        // Decorations
        if (maleDecoration != null)
            maleDecoration.SetActive(genderIndex == 0);
        if (femaleDecoration != null)
            femaleDecoration.SetActive(genderIndex == 1);
    }

    // Set color by index
    public void SetColor(int colorIndex)
    {
        if (bodyRenderer != null && colorIndex >= 0 && colorIndex < colorMaterials.Length)
            bodyRenderer.material = colorMaterials[colorIndex];
    }

    // Called when Photon custom properties are updated
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (photonView.Owner == targetPlayer && changedProps.ContainsKey("ColorIndex"))
        {
            int colorIndex = (int)targetPlayer.CustomProperties["ColorIndex"];
            SetColor(colorIndex);
        }
        if (photonView.Owner == targetPlayer && changedProps.ContainsKey("GenderIndex"))
        {
            int genderIndex = (int)targetPlayer.CustomProperties["GenderIndex"];
            SetGender(genderIndex);
        }
    }

    void Start()
    {
        // On spawn, set appearance from custom properties if available
        if (photonView.Owner.CustomProperties.TryGetValue("GenderIndex", out object genderObj))
        {
            SetGender((int)genderObj);
        }
        if (photonView.Owner.CustomProperties.TryGetValue("ColorIndex", out object colorObj))
        {
            SetColor((int)colorObj);
        }
    }
}
