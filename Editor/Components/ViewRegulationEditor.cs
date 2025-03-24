using UnityEditor;

namespace LandscapeDesignTool.Editor
{
    [CustomEditor(typeof(ViewRegulation))]
    [CanEditMultipleObjects]
    public class ViewRegulationEditor : UnityEditor.Editor
    {
        private ViewRegulationGUI _gui;

        private void Init()
        {
            _gui = new ViewRegulationGUI((ViewRegulation)target);
        }

        public override void OnInspectorGUI()
        {
            if (_gui == null) Init();
            serializedObject.Update();
            _gui.Draw((ViewRegulation)target);
        }

        public void OnSceneGUI()
        {
            if (_gui == null) Init();
            _gui.OnSceneGUI((ViewRegulation)target);
        }
    }

    
}
