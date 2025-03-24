using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LandscapeDesignTool.Editor.WindowTabs
{
    /// <summary>
    /// 視点場作成のGUIを描画します。
    /// </summary>
    public class TabViewPointGenerate : IGuiTabContents
    {
        private EditorWindow _parentWindow;
        private Vector2 _scrollPosition;

        private int selectedIndex;

        private const string ViewPointGroupName = "視点場";
        private const string ViewPointName = "ViewPoint";

        public TabViewPointGenerate(EditorWindow parentWindow)
        {
            _parentWindow = parentWindow;
        }

        private Pose _lastCameraPose;
        public void Update()
        {
            // カメラの位置が変わった際にプレビューの描画を更新するための措置
            var viewPort = LandScapeViewPointEditor.Active?.Target;
            if (viewPort == null)
                return;

            var newCameraPose = new Pose(
                viewPort.transform.position, viewPort.transform.rotation);
            if (_lastCameraPose != newCameraPose)
                _parentWindow.Repaint();

            _lastCameraPose = newCameraPose;
        }

        public void OnGUI()
        {
            LDTTools.CheckTag("ViewPoint");

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawCreatePanel();
            DrawEditPanel();
            DrawDelete();
            DrawCameraPreview();

            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI()
        {
            
        }

        private void DrawCreatePanel()
        {
            EditorGUILayout.Space();
            LandscapeEditorStyle.Header("視点場の作成");

            if (GUILayout.Button("視点場の追加"))
            {
                var viewpointGroup = Object.FindObjectOfType<LandscapeViewPointGroup>()?.gameObject;
                if (!viewpointGroup)
                {
                    viewpointGroup = new GameObject(ViewPointGroupName);
                    viewpointGroup.AddComponent<LandscapeViewPointGroup>();
                }
                GameObject viewPointObject = new GameObject(ViewPointName);
                viewPointObject.transform.parent = viewpointGroup.transform;
                viewPointObject.AddComponent<LandscapeViewPoint>();
                viewPointObject.name = LDTTools.GetNumberWithTag("ViewPoint", "視点場");
                viewPointObject.tag = "ViewPoint";
                viewPointObject.transform.position = Vector3.up * 100f;
                Selection.activeGameObject = viewPointObject;

                InitializeRequiredObjects();
            }
        }

        private void DrawEditPanel()
        {
            DrawList();

            if (LandScapeViewPointEditor.Active?.Target == null)
                return;

            LandScapeViewPointEditor.Active?.OnInspectorGUI();
        }

        private void DrawList()
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag("ViewPoint");

            if (objects.Length == 0)
                return;

            var popupElements = new List<string>();
            for (int i = 0; i < objects.Length; ++i)
            {
                popupElements.Add(objects[i].name);

                // アクティブオブジェクトが外部から変更された際(ヒエラルキーからオブジェクトを選択した際)に表示を更新
                if (Selection.activeGameObject == objects[i])
                    selectedIndex = i;
            }

            if (selectedIndex >= popupElements.Count)
                selectedIndex = popupElements.Count - 1;

            var boldStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("編集対象", boldStyle);

            selectedIndex = EditorGUILayout.Popup("", selectedIndex, popupElements.ToArray());

            Selection.activeGameObject = objects[selectedIndex];
        }

        private void DrawCameraPreview()
        {
            if (LandScapeViewPointEditor.Active?.Target == null)
                return;

            var rect = GUILayoutUtility.GetRect(0, 100, 0, 100);
            rect.height = rect.width * 9 / 16;
            Handles.DrawCamera(rect, LandScapeViewPointEditor.Active.Target.Camera);
        }

        private void DrawDelete()
        {
            if (LandScapeViewPointEditor.Active == null)
                return;

            EditorGUILayout.Space();
            LandscapeEditorStyle.Header("視点場削除");

            GUI.color = Color.red;
            if (GUILayout.Button("選択中の視点場を削除"))
            {
                Object.DestroyImmediate(LandScapeViewPointEditor.Active.Target.gameObject);
                selectedIndex = Mathf.Max(selectedIndex - 1, 0);
            }

            GUI.color = Color.white;
        }

        private void InitializeRequiredObjects()
        {
            if (!GameObject.Find("UI"))
            {
                GameObject ui = new GameObject();
                ui.name = "UI";
                Canvas canvas = ui.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = ui.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                GraphicRaycaster raycaster = ui.AddComponent<GraphicRaycaster>();
            }

            if (!Camera.main.gameObject.GetComponent<WalkThruHandler>())
            {
                Camera.main.gameObject.AddComponent<WalkThruHandler>();
            }

            if (!GameObject.Find("EventSystem"))
            {
                GameObject go = new GameObject();
                go.name = "EventSystem";
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }
    }
}