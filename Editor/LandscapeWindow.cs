using Codice.Client.Common;
using Landscape2.Runtime;
using System;
using UnityEditor;

namespace Landscape2.Editor
{
    /// <summary>
    /// 景観ツールのEditorWindowのエントリーポイントです。
    /// </summary>
    public class LandscapeWindow : EditorWindow
    {
        // private RegulationAreaUI regulationAreaUI;
        // private LineOfSightUI lineOfSightUI;
        private DateTime lastSceneDrawTime;
        
        [MenuItem("PLATEAU/Landscape 2")]
        public static void Open()
        {
            var window = GetWindow<LandscapeWindow>("Landscape 2");
            window.Show();
        }

        private void OnEnable()
        {
            // regulationAreaUI = RegulationAreaUI.CreateForEditorWindow(rootVisualElement);
            // lineOfSightUI = LineOfSightUI.CreateForEditorWindow(rootVisualElement);
            lastSceneDrawTime = DateTime.Now;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView view)
        {
            var now = DateTime.Now;
            var deltaTime = ((float)(now - lastSceneDrawTime).Milliseconds) / 1000f;
            // regulationAreaUI.Update(deltaTime);
            // lineOfSightUI.Update(deltaTime);
            lastSceneDrawTime = now;
        }
    }
}
