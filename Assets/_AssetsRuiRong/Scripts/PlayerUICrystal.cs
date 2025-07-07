using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUICrystal : MonoBehaviour
{
    private Text crystalText;
    
    // Start is called before the first frame update
    void Start()
    {
        Transform textTransform = transform.Find("FP_PlayerUI/RuiRongUI/CrystalNumber");

        if (textTransform != null)
        {
            crystalText = textTransform.GetComponent<Text>();
        }
        else
        {
            Debug.LogWarning("CrystalText UI not found. Please check the path.");
        }

        CrystalManager.Instance.RegisterPlayerUI(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateCrystalUI(int collected, int total)
    {
        if (crystalText != null)
        {
            crystalText.text = $"{collected}/{total}";
        }
    }
}
