// Designed by KINEMATION, 2025.

using KINEMATION.KAnimationCore.Editor.Misc;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEditor;

namespace KINEMATION.ProceduralRecoilAnimationSystem.Editor
{
    [CustomEditor(typeof(RecoilAnimData))]
    public class RecoilAnimDataEditor : UnityEditor.Editor
    {
        private TabInspectorWidget _tabInspectorWidget;

        private void OnEnable()
        {
            _tabInspectorWidget = new TabInspectorWidget(serializedObject);
            _tabInspectorWidget.Init();
        }

        public override void OnInspectorGUI()
        {
            _tabInspectorWidget.Render();
        }
    }
}