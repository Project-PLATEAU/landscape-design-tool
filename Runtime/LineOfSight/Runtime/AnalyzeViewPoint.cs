using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 視点場解析モードにおいてクリックの動作を制御する列挙型
    /// </summary>
    public enum ViewPointSetMode
    {
        None,
        landmark,
        viewPoint
    }

    /// <summary>
    /// 視点場解析に必要なデータの構造体
    /// </summary>
    [Serializable]
    public struct AnalyzeViewPointElements
    {
        // 0 ~ 180(度数)
        public int rangeWidth;
        // 0 ~ 50(m)
        public int rangeHeight;

        // 1〜100。ray射出間隔
        public int raySpan;
        public string startPosName;
        public string endPosName;
        public Vector3 startPos;
        public Vector3 endPos;
        public Color lineColorValid;
        public Color lineColorInvalid;
        public static readonly AnalyzeViewPointElements Empty = new AnalyzeViewPointElements
        {
            rangeWidth = 0,
            rangeHeight = 0,
            raySpan = 1,
            startPosName = "",
            endPosName = "",
            startPos = Vector3.zero,
            endPos = Vector3.zero,
            lineColorValid = Color.clear,
            lineColorInvalid = Color.clear
        };
    }

    /// <summary>
    /// 視点場の解析に関するクラス
    /// </summary>
    public class AnalyzeViewPoint : LineOfSightModeClass
    {
        const float visibleAlpha = 0.2f;
        const float invisibleAlpha = 0f;
        private Camera cam;
        private Ray ray;
        private ViewPointSetMode viewPointSetMode;
        private VisualElement viewPointListPanel;
        private VisualElement viewPointListPanelTitle;
        private VisualElement landMarkListPanel;
        private VisualElement landMarkListPanelTitle;
        private VisualElement analyzeSettingPanel;
        private VisualElement analyzeSettingPanelTitle;

        private LineOfSightUI.ViewStateControl viewPointList_View;
        private LineOfSightUI.ViewStateControl landmarkList_View;
        private LineOfSightUI.ViewStateControl analyzeSettingPanel_View;


        private GameObject targetViewPoint;
        private GameObject targetLandmark;
        private AnalyzeViewPointElements analyzeViewPointData;
        private AnalyzeViewPointElements editViewPointData;
        private LineOfSightDataComponent lineOfSightDataComponent;
        private const string ObjNameLineOfSight = "LineOfSight";

        private readonly Color lineColorValid = new(0, 191f / 255f, 1f, 0.2f);
        private readonly Color lineColorInvalid = new(1f, 140f / 255f, 0f, 0.2f);


        bool visibleValid = false;
        bool visibleInvalid = true;

        public AnalyzeViewPoint(LineOfSightDataComponent lineOfSightDataComponentInstance)
        {
            analyzeViewPointData = new AnalyzeViewPointElements();
            lineOfSightDataComponent = lineOfSightDataComponentInstance;
        }

        public override void OnEnable(VisualElement element)
        {
            viewPointListPanel = element.Q<VisualElement>("ViewPointList");
            viewPointListPanelTitle = element.Q<VisualElement>("Title_ViewList");
            landMarkListPanel = element.Q<VisualElement>("LandMarkList");
            landMarkListPanelTitle = element.Q<VisualElement>("Title_LandmarkList");
            analyzeSettingPanel = element.Q<VisualElement>("AnalyzeSetting");
            analyzeSettingPanelTitle = element.Q<VisualElement>("Title_AnalyzeSetting");

            viewPointList_View = new(viewPointListPanelTitle, viewPointListPanel);
            landmarkList_View = new(landMarkListPanelTitle, landMarkListPanel);
            analyzeSettingPanel_View = new(analyzeSettingPanelTitle, analyzeSettingPanel);

            targetViewPoint = null;
            targetLandmark = null;
            analyzeViewPointData.rangeWidth = 80;
            analyzeViewPointData.rangeHeight = 50;
            analyzeViewPointData.raySpan = 1;
            SetValidAreaColor(visibleValid);
            SetInvalidAreaColor(visibleInvalid);
        }

        public void ClearSetMode()
        {
            viewPointSetMode = ViewPointSetMode.None;
        }

        public void SetLandMark()
        {
            viewPointSetMode = ViewPointSetMode.landmark;
        }

        public void SetViewPoint()
        {
            viewPointSetMode = ViewPointSetMode.viewPoint;
        }

        void SetValidAreaColor(bool state)
        {
            analyzeViewPointData.lineColorValid =
            new Color(
                lineColorValid.r,
                lineColorValid.g,
                lineColorValid.b,
                state ? visibleAlpha : invisibleAlpha
            );

        }

        void SetInvalidAreaColor(bool state)
        {
            analyzeViewPointData.lineColorInvalid =
            new Color(
                lineColorInvalid.r,
                lineColorInvalid.g,
                lineColorInvalid.b,
                state ? visibleAlpha : invisibleAlpha
            );
        }


        /// <summary>
        /// クリックされたポイントを登録する
        /// </summary>
        private void SetTarget()
        {
            cam = Camera.main;
            ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");
                var hitObject = hit.collider.gameObject;
                Debug.Log(hitObject.name);
                if (viewPointSetMode == ViewPointSetMode.viewPoint)
                {
                    var viewPointMarkers = GameObject.Find("ViewPointMarkers");
                    if (hitObject.transform.IsChildOf(viewPointMarkers.transform))
                    {
                        targetViewPoint = hitObject;
                        new_Analyze_Viewpoint.Q<Label>("ViewpointName").text = hitObject.name;
                        viewPointList_View.Show(false);
                        analyzeSettingPanel_View.Show(true);
                        // viewPointListPanel.style.display = DisplayStyle.None;
                        // analyzeSettingPanel.style.display = DisplayStyle.Flex;
                        ClearSetMode();
                    }
                }
                else if (viewPointSetMode == ViewPointSetMode.landmark)
                {
                    var landmarkMarkers = GameObject.Find("LandmarkMarkers");
                    if (hitObject.transform.IsChildOf(landmarkMarkers.transform))
                    {
                        targetLandmark = hitObject;
                        new_Analyze_Viewpoint.Q<Label>("LandmarkName").text = hitObject.name;
                        landmarkList_View.Show(false);
                        analyzeSettingPanel_View.Show(true);
                        ClearSetMode();
                    }
                }
            }
            if (targetLandmark != null && targetViewPoint != null)
            {
                CreateLineOfSight();
            }
        }

        public void UpdateTargets()
        {
            var new_Analyze_Viewpoint = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Viewpoint");
            var viewpointName = new_Analyze_Viewpoint.Q<Label>("ViewpointName").text;
            var landmarkName = new_Analyze_Viewpoint.Q<Label>("LandmarkName").text;

            var viewPointMarkers = GameObject.Find("ViewPointMarkers");
            var landmarkMarkers = GameObject.Find("LandmarkMarkers");

            foreach (Transform t in viewPointMarkers.transform)
            {
                if (t.name == viewpointName)
                {
                    targetViewPoint = t.gameObject;
                    break;
                }
            }

            foreach (Transform t in landmarkMarkers.transform)
            {
                if (t.name == landmarkName)
                {
                    targetLandmark = t.gameObject;
                    break;
                }
            }

        }

        private Vector3 GetTargetPos(GameObject target)
        {
            // WARNING: 行儀が悪い。ViewPoint#GeneratePointMarkerの構成通りでないと動かない
            return target.transform.GetChild(0).position;

        }

        /// <summary>
        /// 見通し解析のラインを生成する(解析を行う)
        /// </summary>
        public float CreateLineOfSight()
        {
            // 見通し解析の初期化
            ClearLineOfSight();
            if (targetViewPoint == null)
            {
                UpdateTargets();
            }

            analyzeViewPointData.startPos = GetTargetPos(targetViewPoint); // targetViewPoint.transform.position;
            analyzeViewPointData.startPosName = targetViewPoint.name;
            analyzeViewPointData.endPos = GetTargetPos(targetLandmark); // targetLandmark.transform.position;
            analyzeViewPointData.endPosName = targetLandmark.name;

            var obj = new GameObject(ObjNameLineOfSight);
            obj.transform.parent = targetViewPoint.transform;

            float rangeWidth = analyzeViewPointData.rangeWidth * Mathf.Deg2Rad;
            int rangeHeight = analyzeViewPointData.rangeHeight;
            Vector3 startPointPos = analyzeViewPointData.startPos;
            Vector3 endPointPos = analyzeViewPointData.endPos;

            float result = -1;
            int circumferenceInterval = Mathf.FloorToInt(rangeWidth * Mathf.Rad2Deg);
            {
                int cfi = Mathf.FloorToInt(rangeWidth * (float)analyzeViewPointData.raySpan * Mathf.Rad2Deg);
                circumferenceInterval = cfi;
            }
            var diff = endPointPos - startPointPos;
            float radius = Vector2.Distance(new Vector2(startPointPos.x, startPointPos.z), new Vector2(endPointPos.x, endPointPos.z));

            Vector3 targetPoint = new Vector3(0, 0, 0);
            for (int i = 0; i < circumferenceInterval; i++)
            {
                for (int j = 0; j < rangeHeight; j++)
                {
                    targetPoint.x = radius * Mathf.Sin(i * rangeWidth / (float)circumferenceInterval - rangeWidth / 2);
                    targetPoint.y = endPointPos.y - startPointPos.y + j - rangeHeight / 2;
                    targetPoint.z = radius * Mathf.Cos(i * rangeWidth / (float)circumferenceInterval - rangeWidth / 2);
                    // 始点から終点のベクトルの角度を求めます。
                    float angle = Vector2.SignedAngle(Vector2.up, new Vector2(diff.x, diff.z));
                    var rotator = Quaternion.AngleAxis(-angle, Vector3.up);
                    targetPoint = rotator * targetPoint + startPointPos;

                    RaycastHit hit;
                    if (RaycastBuildings(targetViewPoint, targetPoint, out hit))
                    {
                        DrawLine(analyzeViewPointData.startPos, hit.point, obj, analyzeViewPointData.lineColorValid);
                        DrawLine(hit.point, targetPoint, obj, analyzeViewPointData.lineColorInvalid);
                    }
                    else
                    {
                        DrawLine(analyzeViewPointData.startPos, targetPoint, obj, analyzeViewPointData.lineColorValid);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 見通し解析のラインを削除する
        /// </summary>
        public void ClearLineOfSight()
        {
            if (targetViewPoint == null)
            {
                return;
            }
            var root = targetViewPoint.transform;
            for (int i = 0; i < root.childCount; ++i)
            {
                var trans = root.GetChild(i);
                string childName = trans.name;
                if (childName == ObjNameLineOfSight)
                {
                    Object.DestroyImmediate(trans.gameObject);
                }
            }
        }

        /// <summary>
        /// ラインを生成する
        /// </summary>
        void DrawLine(Vector3 origin, Vector3 distination, GameObject parent, Color col)
        {
            Vector3[] point = new Vector3[2];
            point[0] = origin;
            point[1] = distination;

            GameObject go = new GameObject("ViewRegurationAreaByLine");

            LineRenderer lineRenderer = go.AddComponent<LineRenderer>();

            lineRenderer.SetPositions(point);
            lineRenderer.positionCount = point.Length;
            lineRenderer.startWidth = 1.0f;
            lineRenderer.endWidth = 1.0f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            lineRenderer.startColor = col;
            lineRenderer.endColor = col;

            go.transform.parent = parent.transform;
        }

        /// <summary>
        /// ラインが建物にあたった時の処理
        /// </summary>
        bool RaycastBuildings(GameObject startPoint, Vector3 destination, out RaycastHit hitInfo)
        {
            bool result = false;

            hitInfo = new RaycastHit();

            var startPos = GetTargetPos(startPoint);

            Vector3 direction = (destination - startPos).normalized;
            float distance = Vector3.Distance(startPos, destination);

            RaycastHit[] hits;
            hits = Physics.RaycastAll(startPos, direction, distance);

            float minDistance = float.MaxValue;
            if (hits.Length <= 0)
                return result;

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.name == startPoint.name)
                    continue;

                int layerIgnoreRaycast = LayerMask.NameToLayer("RegulationArea");

                if (hit.collider.gameObject.layer == layerIgnoreRaycast)
                    continue;

                result = true;

                if (hit.distance >= minDistance)
                    continue;

                hitInfo = hit;
                minDistance = hit.distance;
            }
            return result;
        }

        /// <summary>
        /// 視点場の解析結果を登録する
        /// </summary>
        public AnalyzeViewPointElements RegisterAnalyzeData()
        {
            var keyName = CreateAnalyzeDataDictKey(analyzeViewPointData);
            Debug.Log($"keyName : {keyName}");
            var isAdded = lineOfSightDataComponent.AddAnalyzeViewPoinData(keyName, analyzeViewPointData);
            if (isAdded)
            {
                return analyzeViewPointData;
            }
            else
            {
                return AnalyzeViewPointElements.Empty;
            }
        }

        /// <summary>
        /// 視点場の解析結果を編集する
        /// </summary>
        public (string deleteButtonName, AnalyzeViewPointElements analyzeViewPointData) EditAnalyzeData()
        {
            var deleteButtonName = DeleteAnalyzeData();
            var keyName = CreateAnalyzeDataDictKey(analyzeViewPointData);
            var isAdded = lineOfSightDataComponent.AddAnalyzeViewPoinData(keyName, analyzeViewPointData);

            if (isAdded)
            {
                return (deleteButtonName, analyzeViewPointData);
            }
            else
            {
                return ("", AnalyzeViewPointElements.Empty);
            }
        }

        /// <summary>
        /// 視点場の解析結果を削除する
        /// </summary>
        public string DeleteAnalyzeData()
        {
            // ゲームオブジェクトの削除
            ClearLineOfSight();
            // データの削除
            if (editViewPointData.Equals(AnalyzeViewPointElements.Empty))
            {
                return "";
            }
            var keyName = CreateAnalyzeDataDictKey(editViewPointData);
            var isRemoved = lineOfSightDataComponent.RemoveAnalyzeViewPoinData(keyName);
            editViewPointData = AnalyzeViewPointElements.Empty;
            if (isRemoved)
            {
                return keyName;
            }
            else
            {
                return "";
            }
        }
        
        /// <summary>
        /// 眺望対象解析のスライダーの登録
        /// </summary>
        public void SetAnalyzeRange()
        {
            var slider_Viewpoint = analyzeSettingPanel.Q<VisualElement>("Slider_Viewpoint");
            var horizontalSlider = slider_Viewpoint.Q<SliderInt>("HorizontalSlider");
            horizontalSlider.lowValue = 10;
            horizontalSlider.highValue = 180;
            horizontalSlider.value = analyzeViewPointData.rangeWidth;
            horizontalSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeViewPointData.rangeWidth = evt.newValue;
                CreateLineOfSight();
            });

            var verticalSlider = slider_Viewpoint.Q<SliderInt>("VerticalSlider");
            verticalSlider.lowValue = 0;
            verticalSlider.highValue = 150;
            verticalSlider.value = analyzeViewPointData.rangeHeight;
            verticalSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeViewPointData.rangeHeight = evt.newValue;
                CreateLineOfSight();
            });

            var raySpanSlider = slider_Viewpoint.Q<SliderInt>("RaySpanSlider");
            raySpanSlider.lowValue = 1;
            raySpanSlider.highValue = 10;
            raySpanSlider.value = analyzeViewPointData.raySpan;
            raySpanSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeViewPointData.raySpan = evt.newValue;
                CreateLineOfSight();
            });

            var visibleCheckbox = slider_Viewpoint.Q<Toggle>("validVisibleCheck");
            visibleCheckbox.value = visibleValid;
            visibleCheckbox.RegisterValueChangedCallback(evt =>
            {
                visibleValid = evt.newValue;
                SetValidAreaColor(visibleValid);
                CreateLineOfSight();
            });

            var invisibleCheckbox = slider_Viewpoint.Q<Toggle>("invalidVisibleCheck");
            invisibleCheckbox.value = visibleInvalid;
            invisibleCheckbox.RegisterValueChangedCallback(evt =>
            {
                visibleInvalid = evt.newValue;

                SetInvalidAreaColor(visibleInvalid);
                CreateLineOfSight();
            });



        }

        /// <summary>
        /// 登録キーの生成
        /// </summary>
        private string CreateAnalyzeDataDictKey(AnalyzeViewPointElements analyzeViewPointElements)
        {
            var key = analyzeViewPointElements.startPosName + analyzeViewPointElements.endPosName + analyzeViewPointElements.rangeWidth + analyzeViewPointElements.rangeHeight;
            return key;
        }

        /// <summary>
        /// 解析結果のボタンが押されたときの処理
        /// </summary>
        public void ButtonAction(AnalyzeViewPointElements analyzeViewPointElements)
        {
            editViewPointData = analyzeViewPointElements;
            analyzeViewPointData = analyzeViewPointElements;
            var viewPointMarkers = GameObject.Find("ViewPointMarkers");
            foreach (Transform child in viewPointMarkers.transform)
            {
                if (child.gameObject.name == analyzeViewPointData.startPosName)
                {
                    targetViewPoint = child.gameObject;
                }
            }
            var landmarkMarkers = GameObject.Find("LandmarkMarkers");
            foreach (Transform child in landmarkMarkers.transform)
            {
                if (child.gameObject.name == analyzeViewPointData.endPosName)
                {
                    targetLandmark = child.gameObject;
                }
            }
            if (targetViewPoint == null || targetLandmark == null)
            {
                return;
            }
            CreateLineOfSight();
        }
        
        /// <summary>
        /// 編集可能か
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CanEdit(string name)
        {
            var data = lineOfSightDataComponent
                .AnalyzeViewPointDatas.Find(point => point.Name == name);
            return data.IsProject(ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID);
        }

        public override void OnSelect()
        {
            Debug.Log($"OnSelect");
            SetTarget();
        }

        public override void OnDisable()
        {
        }
    }
}
