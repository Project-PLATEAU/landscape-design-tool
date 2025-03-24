using System.Linq;
using UnityEditor;
using UnityEngine;
using LandscapeDesignTool.Editor.WindowTabs;

namespace LandscapeDesignTool.Editor
{
#if UNITY_EDITOR
    public class LandscapeConsult : EditorWindow
    {


        private int _mode = 0;
        int _buildmode = 0;
        private GameObject _rootHeightArea;
        private const string HeightAreaGroupName = "Height Restricted Areas";
        private const string HeightAreaName = "Height Restricted Area";


        /*
         * layer
         */
        string[] layerName = { "RegulationArea" };
        int[] layerId = { 30 };
        
        private readonly IGuiTabContents[] _tabContents;

        int _regulationType;
        float _regulationHeight;

        bool _point_edit_in = false;

        private readonly string[] _tabToggles = { "視点場作成", "Shapefile読込","天候と時間" };
        private int _tabIndex;

        public LandscapeConsult()
        {
            _tabContents = new IGuiTabContents[]
            {
                new TabViewPointGenerate(this),
                new TabShapefileLoad(),
                new TabWeatherAndTime()
            };
        }

        private void Awake()
        {

            LDTTools.SetUI();
        }

        [MenuItem("PLATEAU/景観まちづくり/景観協議")]
        public static void ShowWindow()
        {
            TagAdder.ConfigureTags();
            EditorWindow.GetWindow(typeof(LandscapeConsult), true, "景観協議画面");
        }

        private void Update()
        {
            _tabContents[_tabIndex].Update();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck(); EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tabIndex = GUILayout.Toolbar(_tabIndex, _tabToggles, new GUIStyle(EditorStyles.toolbarButton), GUI.ToolbarButtonSize.FitToContents);
            }

            var style = new GUIStyle(EditorStyles.label);
            style.richText = true;
            _tabContents[_tabIndex].OnGUI();

        }

        private void OnInspectorUpdate()
        {
            ((TabWeatherAndTime)_tabContents.First(c => c is TabWeatherAndTime)).OnInspectorUpdate();
        }
    }


#endif
}
