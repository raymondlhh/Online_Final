// Designed by KINEMATION, 2024.

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace KINEMATION.ScriptableWidget
{
    public class ScriptableComponentListWidget
    {
        public float headerSpaceRatio = 0.5f;
        
        public delegate void ComponentAction();
        
        public delegate void DrawElementHeader(SerializedProperty property, Rect rect);
        public delegate void SelectionAction(int selectedIndex);

        public ComponentAction onComponentAdded;
        public ComponentAction onComponentPasted;
        public ComponentAction onComponentRemoved;
        public DrawElementHeader onDrawComponentHeader;
        public SelectionAction onComponentSelected;
        
        private SerializedObject _serializedObject;
        private SerializedProperty _componentsProperty;
        private ScriptableObject _asset;
        
        private Type _collectionType;
        
        private List<Editor> _editors;

        private bool _isInitialized;
        private ReorderableList _componentsList;

        private ScriptableComponentEditorWindow _componentEditorWindow;
        private GUIStyle _elementButtonStyle;

        private Type[] _componentTypes;
        private string _friendlyComponentName;

        private bool _useStandaloneWindow;
        private int _editorIndex = -1;

        public ScriptableComponentListWidget(string friendlyComponentName)
        {
            _friendlyComponentName = friendlyComponentName;
        }

        public void AddComponent(Type type)
        {
            _serializedObject.Update();
            
            ScriptableObject newComponent = CreateNewComponent(type);
            
            Undo.RegisterCreatedObjectUndo(newComponent, "Add Component");
            AssetDatabase.AddObjectToAsset(newComponent, _asset);
            
            _componentsProperty.arraySize++;
            var componentProp = _componentsProperty.GetArrayElementAtIndex(_componentsProperty.arraySize - 1);
            componentProp.objectReferenceValue = newComponent;

            _editors.Add(Editor.CreateEditor(newComponent));
            _serializedObject.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);
            
            onComponentAdded?.Invoke();
        }
        
        private ScriptableObject CreateNewComponent(Type type)
        {
            var instance = ScriptableObject.CreateInstance(type);
            instance.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            instance.name = type.Name;
            return instance;
        }

        private void OnTypeSelected(object userData, string[] options, int selected)
        {
            AddComponent(_componentTypes[selected]);
        }

        public void RemoveComponent(int index)
        {
            if (_componentEditorWindow != null && _editors[index] == _componentEditorWindow.GetEditor())
            {
                _componentEditorWindow.Close();
                _editorIndex = -1;
            }
            
            _editors.RemoveAt(index);
            _serializedObject.Update();

            _editorIndex--;
            
            var property = _componentsProperty.GetArrayElementAtIndex(index);
            var component = property.objectReferenceValue;
            
            property.objectReferenceValue = null;
            _componentsProperty.DeleteArrayElementAtIndex(index);
            
            _serializedObject.ApplyModifiedProperties();
            Undo.DestroyObjectImmediate(component);
            
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            
            onComponentRemoved?.Invoke();
        }
        
        private void CopyComponent(Object component)
        {
            string typeName = component.GetType().AssemblyQualifiedName;
            string typeData = JsonUtility.ToJson(component);
            EditorGUIUtility.systemCopyBuffer = $"{typeName}|{typeData}";
        }

        private bool CanPaste(Object component)
        {
            if (string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer)) return false;

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            int separator = clipboard.IndexOf('|');

            if (separator < 0) return false;

            return component.GetType().AssemblyQualifiedName == clipboard.Substring(0, separator);
        }

        private void PasteComponent(Object component)
        {
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            string typeData = clipboard.Substring(clipboard.IndexOf('|') + 1);
            Undo.RecordObject(component, "Paste Settings");
            JsonUtility.FromJsonOverwrite(typeData, component);
        }

        private void OnContextMenuSelection(object userData, string[] options, int selected)
        {
            int index = (int) userData;
            Object component = _componentsProperty.GetArrayElementAtIndex(index).objectReferenceValue;
            
            if (selected == 0)
            {
                CopyComponent(component);
                return;
            }

            if (!CanPaste(component)) return;
            
            PasteComponent(component);
            onComponentPasted?.Invoke();
        }

        private void SetupReorderableList(string targetSerializedPropertyName)
        {
            _componentsList = new ReorderableList(_serializedObject,
                _serializedObject.FindProperty(targetSerializedPropertyName),
                true, false, false, true);

            _componentsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _componentsList.serializedProperty.GetArrayElementAtIndex(index);

                float singleHeight = EditorGUIUtility.singleLineHeight;
                rect.y += 1;

                float labelWidth = rect.width * headerSpaceRatio;
                float buttonWidth = rect.width * (1f - headerSpaceRatio);
                
                Rect labelRect = new Rect(rect.x, rect.y, labelWidth, singleHeight);
                Rect buttonRect = new Rect(rect.x + labelWidth, rect.y, buttonWidth, singleHeight);

                string elementName = element.objectReferenceValue.name;
                if (onDrawComponentHeader != null)
                {
                    onDrawComponentHeader.Invoke(element, labelRect);
                }
                else
                {
                    EditorGUI.LabelField(labelRect, elementName);
                }
                
                if (GUI.Button(buttonRect, "Edit Layer", EditorStyles.miniButton))
                {
                    if (_useStandaloneWindow)
                    {
                        if (_componentEditorWindow == null)
                        {
                            _componentEditorWindow = ScriptableComponentEditorWindow.CreateWindow();
                        }

                        _componentEditorWindow.RefreshEditor(_editors[index], $"{elementName} Editor");

                        _componentEditorWindow.Show();
                        _componentEditorWindow.Repaint();
                    }
                    else
                    {
                        _editorIndex = index;
                    }
                    
                    onComponentSelected?.Invoke(index);
                }
                
                if (Event.current.type == EventType.MouseUp && Event.current.button == 1 
                    && labelRect.Contains(Event.current.mousePosition))
                {
                    GUIContent[] menuOptions = new GUIContent[]
                    {
                        new GUIContent("Copy"),
                        new GUIContent("Paste")
                    };
                
                    EditorUtility.DisplayCustomMenu(new Rect(Event.current.mousePosition, Vector2.zero), 
                        menuOptions, -1, OnContextMenuSelection, index);
                }
            };

            _componentsList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
            {
                Editor editorToMove = _editors[oldIndex];
                
                _editors.RemoveAt(oldIndex);
                if (newIndex > oldIndex)
                {
                    if (newIndex > _editors.Count - 1)
                    {
                        _editors.Add(editorToMove);
                        return;
                    }
                }
                
                _editors.Insert(newIndex, editorToMove);
            };

            _componentsList.onRemoveCallback = list =>
            {
                RemoveComponent(list.index);
            };
        }
        
        public void Init(SerializedObject serializedObject, Type collectionType, string collectionName, 
            bool standaloneWindow = true)
        {
            _serializedObject = serializedObject;
            _componentsProperty = serializedObject.FindProperty(collectionName);
            _collectionType = collectionType;
            _useStandaloneWindow = standaloneWindow;
            
            Assert.IsNotNull(_componentsProperty);
            Assert.IsNotNull(_collectionType);
            
            _asset = _serializedObject.targetObject as ScriptableObject;
            if (_asset == null)
            {
                Debug.LogError($"{serializedObject.targetObject.name} is not a Scriptable Object!");
                return;
            }

            if (!_componentsProperty.isArray)
            {
                Debug.LogError($"{_componentsProperty.displayName} is not an array!");
                return;
            }

            List<Type> collectionTypes = new List<Type>();
            var allCollectionTypes = TypeCache.GetTypesDerivedFrom(_collectionType).ToArray();

            foreach (var type in allCollectionTypes)
            {
                if(type.IsAbstract) continue;
                collectionTypes.Add(type);
            }
            
            _componentTypes = collectionTypes.ToArray();
            _editors = new List<Editor>();

            // Create editors for the current components.
            int arraySize = _componentsProperty.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty element = _componentsProperty.GetArrayElementAtIndex(i);
                _editors.Add(Editor.CreateEditor(element.objectReferenceValue));
            }
            
            SetupReorderableList(collectionName);
            _isInitialized = true;
        }

        public void OnGUI()
        {
            if (!_isInitialized) return;
            
            _componentsList.DoLayoutList();

            EditorGUILayout.Space();
            
            if (GUILayout.Button($"Add {_friendlyComponentName}", EditorStyles.miniButton))
            {
                int count = _componentTypes.Length;
                
                GUIContent[] menuOptions = new GUIContent[count];
                for (int i = 0; i < count; i++)
                {
                    menuOptions[i] = new GUIContent(_componentTypes[i].Name);
                }
                
                EditorUtility.DisplayCustomMenu(new Rect(Event.current.mousePosition, Vector2.zero), 
                    menuOptions, -1, OnTypeSelected, null);
            }

            if (_useStandaloneWindow || _editorIndex < 0 || _editors.Count == 0)
            {
                return;
            }
            
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            _editors[_editorIndex].OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }
    }
}
