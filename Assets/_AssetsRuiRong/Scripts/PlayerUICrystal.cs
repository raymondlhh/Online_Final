using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUICrystal : MonoBehaviour
{
    public Text crystalText;


    
    // Start is called before the first frame update
    void Start()
    {
        Transform textTransform = transform.root.Find("FP_PlayerUI/RuiRongUI/CrystalNumber");

        if (textTransform != null)
        {
            crystalText = textTransform.GetComponent<Text>();
            crystalText.text = "0/3 (Test)";
            Debug.Log(" Text found and updated.");
        }
        else
        {
            Debug.LogError("Crystal UI text not found.");
        }

        UpdateCrystalUI(0, 3);
        
        CrystalManager.Instance.RegisterPlayerUI(this);

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateCrystalUI(int collected, int total)
    {
        Debug.Log("UpdateCrystalUI!");
        
        if (crystalText != null)
        {
            crystalText.text = $"{collected}/{total}";
        }
    }
}
