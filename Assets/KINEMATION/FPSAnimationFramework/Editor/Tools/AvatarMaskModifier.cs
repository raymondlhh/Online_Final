// Designed by KINEMATION, 2024.

using System;
using KINEMATION.KAnimationCore.Editor.Misc;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Editor.Tools
{
    public class AvatarMaskModifier : IEditorTool
    {
        private Transform _root;
        private Transform _boneToAdd;
        private AvatarMask _maskToModify;
        
        public void Render()
        {
            EditorGUILayout.HelpBox("This tool adds a Transform to the Avatar Mask. " 
                                    + "Useful if you need to include a non-skeletal object in your mask.", 
                MessageType.Info);
            
            _root = EditorGUILayout.ObjectField("Root", _root, typeof(Transform), true)
                as Transform;
            
            _boneToAdd =
                EditorGUILayout.ObjectField("Bone To Add", _boneToAdd, typeof(Transform), true)
                    as Transform;

            _maskToModify =
                EditorGUILayout.ObjectField("Upper Body Mask", _maskToModify, typeof(AvatarMask), true) 
                    as AvatarMask;

            if (_boneToAdd == null)
            {
                EditorGUILayout.HelpBox("Select the Bone transform", MessageType.Warning);
                return;
            }
            
            if (_maskToModify == null)
            {
                EditorGUILayout.HelpBox("Select the Avatar Mask", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Add Bone"))
            {
                for (int i = _maskToModify.transformCount - 1; i >= 0; i--)
                {
                    if (_maskToModify.GetTransformPath(i).EndsWith(_boneToAdd.name))
                    {
                        return;
                    }
                }

                _maskToModify.AddTransformPath(_boneToAdd, false);

                if (_root == null) return;

                string path = _maskToModify.GetTransformPath(_maskToModify.transformCount - 1);
                string[] array = path.Split("/");
                int rootIndex = Array.IndexOf(array, _root.name);

                if (rootIndex == -1 || rootIndex == array.Length - 1) return;
                
                path = String.Join("/", array, rootIndex + 1, array.Length - rootIndex - 1);
                
                _maskToModify.SetTransformPath(_maskToModify.transformCount - 1, path);
            }
        }
    }
}