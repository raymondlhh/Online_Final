using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShip : MonoBehaviour
{
    public Animator shipAnimator;
    public bool StartShip = false;

    // Start is called before the first frame update
    void Start()
    {
        if (shipAnimator == null)
        {
            shipAnimator = GetComponent<Animator>();
        }

        // Disable the Animator at start
        if (shipAnimator != null)
        {
            shipAnimator.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (StartShip && shipAnimator != null && !shipAnimator.enabled)
        {
            shipAnimator.enabled = true;
            // Optional: trigger a specific animation
            // shipAnimator.Play("YourAnimationName");
        }
    }
}
