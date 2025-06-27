// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Editor.Misc;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Collections.Generic;
using System.Linq;
using KINEMATION.KAnimationCore.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(KRigElementChain))]
    public class ElementChainDrawer : PropertyDrawer
    {
        private CustomElementChainDrawerAttribute GetCustomChainAttribute()
        {
            CustomElementChainDrawerAttribute attr = null;

            var attributes = fieldInfo.GetCustomAttributes(true);
            foreach (var customAttribute in attributes)
            {
                attr = customAttribute as CustomElementChainDrawerAttribute;
                if (attr != null) break;
            }
            
            return attr;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            KRig rig = RigDrawerUtility.TryGetRigAsset(fieldInfo, property);
            
            SerializedProperty elementChain = property.FindPropertyRelative("elementChain");
            SerializedProperty chainName = property.FindPropertyRelative("chainName");
            
            if (rig != null)
            {
                var rigHierarchy = rig.rigHierarchy;
                
                float labelWidth = EditorGUIUtility.labelWidth;
                var customChain = GetCustomChainAttribute();
                
                Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
                Rect buttonRect = position;
                
                string buttonText = $"Edit {chainName.stringValue}";

                if (customChain is {drawLabel: true})
                {
                    EditorGUI.PrefixLabel(labelRect, label);
                    labelRect.x += labelRect.width;
                    labelRect.width = (position.width - labelWidth) / 2f;

                    buttonRect.x = labelRect.x;
                    buttonRect.width = position.width - labelWidth;
                    
                    buttonText = $"Edit {label.text}";
                }

                if (customChain is {drawTextField: true})
                {
                    chainName.stringValue = EditorGUI.TextField(labelRect, chainName.stringValue);

                    buttonRect.x = labelRect.x + labelRect.width;
                    buttonRect.width = position.width - (buttonRect.x - position.x);
                    
                    buttonText = "Edit";
                }
                
                if (GUI.Button(buttonRect, buttonText))
                {
                    List<int> selectedIds = new List<int>();
                    
                    // Get the active element indexes.
                    int arraySize = elementChain.arraySize;
                    for (int i = 0; i < arraySize; i++)
                    {
                        var indexProp 
                            = elementChain.GetArrayElementAtIndex(i).FindPropertyRelative("index");
                        selectedIds.Add(indexProp.intValue + 1);
                    }
                    
                    var elementNames = rigHierarchy.Select(element => element.name).ToList();
                    KSelectorWindow.ShowWindow(ref elementNames, ref rig.rigDepths,
                        (selectedName, selectedIndex) => { },
                        items =>
                        {
                            elementChain.ClearArray();

                            foreach (var selection in items)
                            {
                                elementChain.arraySize++;
                                int lastIndex = elementChain.arraySize - 1;
                                
                                var element = elementChain.GetArrayElementAtIndex(lastIndex);
                                var name = element.FindPropertyRelative("name");
                                var index = element.FindPropertyRelative("index");

                                name.stringValue = selection.Item1;
                                index.intValue = selection.Item2;
                            }
                            
                            property.serializedObject.ApplyModifiedProperties();
                        },
                        true, selectedIds, "Element Chain Selection"
                    );
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }

            EditorGUI.EndProperty();
        }
    }
}