using LandScapeDesignTool;
using UnityEditor;
using UnityEngine;

namespace LandscapeDesignTool.Editor.WindowTabs
{
    public class TabHeightRegulationGenerate : IGuiTabContents
    {
        float _heightAreaHeight = 30.0f;
        float _heightAreaDiameter = 100.0f;
        bool _heightRegulationAreaEdit = false;
        // Vector3 _targetViewPoint;
        HeightRegulationAreaHandler _heightRegulationArea;
        Color _areaColor = new Color(0, 1, 1, 0.5f);
        bool _editMode = false;
        
        private const int LayerIdGround = 31;
        
        public void OnGUI()
        {
            LDTTools.CheckTag("HeightRegulationArea");
            EditorGUILayout.Space();
            
            LandscapeEditorStyle.Header("表示設定");
            LandscapeEditorStyle.ButtonSwitchDisplay(HeightRegulationRendererSetActive);
            
            LandscapeEditorStyle.Header("眺望対象中心の高さ制限エリア");
            _heightAreaHeight = EditorGUILayout.FloatField("高さ", _heightAreaHeight);
            _heightAreaDiameter = EditorGUILayout.FloatField("直径", _heightAreaDiameter);
            _areaColor = EditorGUILayout.ColorField("色の設定", _areaColor);

            if (_heightRegulationAreaEdit)
            {
                if (_editMode)
                {
                    if (GUILayout.Button("編集完了"))
                    {
                        _editMode = false;
                        _heightRegulationAreaEdit = false;

                        _heightRegulationArea.SetupRegulationArea(_heightAreaDiameter, _areaColor, _heightAreaHeight);
                    }
                }
                else
                {
                    GUI.color = Color.green;
                    if (GUILayout.Button("高さ制限エリア作成"))
                    {
                        _heightRegulationAreaEdit = false;
                        _heightRegulationArea.SetupRegulationArea(_heightAreaDiameter, _areaColor, _heightAreaHeight);
                    }

                    GUI.color = Color.white;
                    if (GUILayout.Button("キャンセル"))
                    {
                        _heightRegulationAreaEdit = false;
                    }
                }
            }
            else
            {
                if (_editMode)
                {
                    if (GUILayout.Button("編集完了"))
                    {
                        _editMode = false;
                        _heightRegulationAreaEdit = false;
                        _heightRegulationArea.SetupRegulationArea(_heightAreaDiameter, _areaColor, _heightAreaHeight);
                    }
                }
                else
                {
                    if (GUILayout.Button("眺望対象(中心地点)を選択"))
                    {
                        GUI.color = Color.white;
                        GameObject grp = GameObject.Find("HeitRegurationAreaGroup");
                        if (!grp)
                        {
                            _heightRegulationAreaEdit = true;
                            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            cylinder.layer = LayerMask.NameToLayer("RegulationArea");
                            HeightRegulationAreaHandler area = cylinder.AddComponent<HeightRegulationAreaHandler>();
                            _heightRegulationArea = area;
                            cylinder.transform.localScale = new Vector3(0, 0, 0);
                            cylinder.name = LDTTools.GetNumberWithTag("HeightRegulationArea", "高さ制限エリア");
                            cylinder.tag = "HeightRegulationArea";
                            
                            // 円柱にデフォルトで付いているカプセルコライダーでは上の方をクリックしにくくなるので、MeshColliderに置き換えます。
                            Object.DestroyImmediate(cylinder.GetComponent<CapsuleCollider>());
                            var meshCollider = cylinder.AddComponent<MeshCollider>();
                            meshCollider.convex = true;
                            

                            Selection.activeGameObject = cylinder;
                        }
                    }

                    LandscapeEditorStyle.Header("眺望対象中心の高さ制限エリアの編集");

                    HeightRegulationAreaList();
                }
            }
        }

        

        public void OnSceneGUI()
        {
            if (_heightRegulationAreaEdit)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                var ev = Event.current;
                if (ev.type == EventType.MouseDown)
                {
                    RaycastHit hit;
                    int layerMask = 1 << LayerIdGround;
                    Vector3 mousePosition = Event.current.mousePosition;
                    Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

                    if(Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
                    {
                        // _targetViewPoint = hit.point;
                        _heightRegulationArea.SetPoint(hit.point);
                    }
                }
            }
        }

        public void Update()
        {
            
        }

        void HeightRegulationAreaList()
        {

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("編集は下記リストより選択してください", MessageType.Info);
            GameObject[] objects = GameObject.FindGameObjectsWithTag("HeightRegulationArea");
            int n = 0;
            foreach (var obj in objects)
            {
                if (GUILayout.Button(obj.name))
                {
                    _editMode = true;
                    Selection.activeGameObject = objects[n];
                    HeightRegulationAreaHandler harea = objects[n].GetComponent<HeightRegulationAreaHandler>();
                    _heightAreaHeight = harea.GetHeight();
                    _heightAreaDiameter = harea.GetDiameter();
                    _areaColor = harea.GetColor();
                    // _targetViewPoint = harea.GetPoint();
                    _heightRegulationArea = harea;
                }

                n++;
            }
        }

        private static void HeightRegulationRendererSetActive(bool isActive)
        {
            var regulations = Object.FindObjectsOfType<HeightRegulationAreaHandler>();
            foreach (var reg in regulations)
            {
                var renderer = reg.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                renderer.enabled = isActive;
            }
        }
    }
}