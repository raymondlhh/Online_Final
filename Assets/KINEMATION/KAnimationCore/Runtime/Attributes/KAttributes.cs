// Designed by KINEMATION, 2024.

using System;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class CurveSelectorAttribute : PropertyAttribute
    {
        public bool useAnimator;
        public bool usePlayables;
        public bool useInput;
        
        public CurveSelectorAttribute(bool useAnimator = true, bool usePlayables = true, bool useInput = true)
        {
            this.useAnimator = useAnimator;
            this.usePlayables = usePlayables;
            this.useInput = useInput;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class InputProperty : PropertyAttribute { }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RigAssetSelectorAttribute : PropertyAttribute
    {
        public string assetName;
        
        public RigAssetSelectorAttribute(string rigName = "")
        {
            assetName = rigName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ElementChainSelectorAttribute : RigAssetSelectorAttribute
    {
        public ElementChainSelectorAttribute(string rigName = "")
        {
            assetName = rigName;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ReadOnlyAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class UnfoldAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TabAttribute : PropertyAttribute
    {
        public string tabName;

        public TabAttribute(string tabName)
        {
            this.tabName = tabName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class CustomElementChainDrawerAttribute : PropertyAttribute
    {
        public bool drawLabel;
        public bool drawTextField;

        public CustomElementChainDrawerAttribute(bool drawLabel, bool drawTextField)
        {
            this.drawLabel = drawLabel;
            this.drawTextField = drawTextField;
        }
    }

    public class KAttributes { }
}