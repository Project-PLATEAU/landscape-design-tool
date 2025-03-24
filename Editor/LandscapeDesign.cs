using System.Collections.Generic;
using System.Linq;
using LandscapeDesignTool.Editor.WindowTabs;
using UnityEngine;
using UnityEditor;


namespace LandscapeDesignTool.Editor
{
#if UNITY_EDITOR
    public class LandscapeDesign : EditorWindow
    {

        private readonly string[] _tabToggles =
            { "視点場作成", "規制エリア作成", "眺望規制作成", "高さ制限エリア作成", "ShapeFile読込", "ShapeFile書き出し" };

        private readonly IGuiTabContents[] _tabContents;

        private int _tabIndex;

        public LandscapeDesign()
        {
            _tabContents = new IGuiTabContents[]
            {
                new TabViewPointGenerate(this),
                new TabRegulationAreaGenerate(this),
                new TabViewportRegulationGenerate(),
                new TabHeightRegulationGenerate(),
                new TabShapefileLoad(),
                new TabRegulationAreaExport()
            };
        }
        private void Awake()
        {

            LDTTools.SetUI();
        }



        [MenuItem("PLATEAU/景観まちづくり/景観計画")]
        public static void ShowWindow()
        {
            TagAdder.ConfigureTags();
            GetWindow(typeof(LandscapeDesign), true, "景観計画画面");
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            _tabContents[_tabIndex].OnSceneGUI();
        }

        private void Update()
        {
            _tabContents[_tabIndex].Update();
        }

        private void OnGUI()
        {

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tabIndex = GUILayout.Toolbar(_tabIndex, _tabToggles, new GUIStyle(EditorStyles.toolbarButton),
                    GUI.ToolbarButtonSize.FitToContents);
            }
            _tabContents[_tabIndex].OnGUI();
        }
    }
}


#endif