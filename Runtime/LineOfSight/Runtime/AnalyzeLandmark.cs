using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 眺望対象解析モードにおいてクリックの動作を制御する列挙型
    /// </summary>
    public enum LandmarkSetMode
    {
        None,
        landmark
    }

    /// <summary>
    /// 眺望対象解析に必要なデータの構造体
    /// </summary>
    [Serializable]
    public struct AnalyzeLandmarkElements
    {
        public int rangeRadius;
        public int rangeUp;
        public int rangeDown;

        public int raySpan;
        public string startPosName;
        public Vector3 startPos;
        public Color lineColorValid;
        public Color lineColorInvalid;
        public static readonly AnalyzeLandmarkElements Empty = new AnalyzeLandmarkElements
        {
            rangeRadius = 0,
            rangeUp = 0,
            rangeDown = 0,
            raySpan = 1,
            startPosName = "",
            startPos = Vector3.zero,
            lineColorValid = Color.clear,
            lineColorInvalid = Color.clear
        };
    }

    /// <summary>
    /// 眺望対象の解析に関するクラス
    /// </summary>
    public class AnalyzeLandmark : LineOfSightModeClass
    {
        const float visibleAlpha = 0.2f;
        const float invisibleAlpha = 0f;

        /// <summary>
        /// 可視化範囲最低距離(m)
        /// </summary>
        const int LandmarkViewMinDistance = 100;
        /// <summary>
        /// 可視化範囲最長距離(m)
        /// </summary>
        const int LandmarkViewMaxDistance = 2000;

        /// <summary>
        /// 可視化ray角度。360をLandmarkViewRayInterval分割して表示。最大360
        /// </summary>
        const int LandmarkViewRayInterval = 96;

        readonly Color lineColorValid = new(0, 191f / 255f, 1, 0f);
        readonly Color lineColorInvalid = new(1, 140f / 255, 0, 0.2f);


        private Camera cam;
        private Ray ray;
        private LandmarkSetMode landmarkSetMode;
        private VisualElement analyzeSettingPanel;
        private GameObject targetLandmark;
        private AnalyzeLandmarkElements analyzeLandmarkData;
        private AnalyzeLandmarkElements editLandmarkData;
        private LineOfSightDataComponent lineOfSightDataComponent;
        private const string ObjNameLineOfSight = "LineOfSight";

        LineOfSightUI.ViewStateControl landmarkListPanel_View;
        LineOfSightUI.ViewStateControl analyzeSettingPanel_View;

        bool visibleValid = false;
        bool visibleInvalid = true;

        public AnalyzeLandmark(LineOfSightDataComponent lineOfSightDataComponentInstance)
        {
            analyzeLandmarkData = new AnalyzeLandmarkElements();
            lineOfSightDataComponent = lineOfSightDataComponentInstance;
        }

        public override void OnEnable(VisualElement element)
        {
            analyzeSettingPanel = element.Q<VisualElement>("AnalyzeSetting");

            var landMarkListPanel = element.Q<VisualElement>("LandMarkList");
            var landmarkListPanelTitle = element.Q<VisualElement>("Title_LandmarkList");
            var analyzeSettingPanelTitle = element.Q<VisualElement>("Title_AnalyzeSetting");
            landmarkListPanel_View = new(landmarkListPanelTitle, landMarkListPanel);
            analyzeSettingPanel_View = new(analyzeSettingPanelTitle, analyzeSettingPanel);

            // 構造体の初期化
            analyzeLandmarkData.rangeRadius = LandmarkViewMinDistance;
            analyzeLandmarkData.rangeUp = 1;
            analyzeLandmarkData.rangeDown = 1;
            analyzeLandmarkData.raySpan = 1;
            analyzeLandmarkData.lineColorValid = lineColorValid;
            analyzeLandmarkData.lineColorInvalid = lineColorInvalid;
        }

        public void ClearSetMode()
        {
            landmarkSetMode = LandmarkSetMode.None;
        }

        public void SetLandMark()
        {
            landmarkSetMode = LandmarkSetMode.landmark;
        }

        void SetValidAreaColor(bool state)
        {
            analyzeLandmarkData.lineColorValid =
            new Color(
                lineColorValid.r,
                lineColorValid.g,
                lineColorValid.b,
                state ? visibleAlpha : invisibleAlpha
            );

        }

        void SetInvalidAreaColor(bool state)
        {
            analyzeLandmarkData.lineColorInvalid =
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
                var new_Analyze_Landmark = analyzeSettingPanel.Q<VisualElement>("New_Analyze_Landmark");
                var hitObject = hit.collider.gameObject;
                if (landmarkSetMode == LandmarkSetMode.landmark)
                {
                    var landmarkMarkers = GameObject.Find("LandmarkMarkers");
                    if (hitObject.transform.IsChildOf(landmarkMarkers.transform))
                    {
                        targetLandmark = hitObject;
                        new_Analyze_Landmark.Q<Label>("LandmarkName").text = hitObject.name;
                        landmarkListPanel_View.Show(false);
                        analyzeSettingPanel_View.Show(true);

                        ClearSetMode();
                    }
                }
            }
            if (targetLandmark != null)
            {
                CreateLineOfSight();
            }
        }

        private Vector3 GetTargetPos(GameObject target)
        {
            // WARNING: 行儀が悪い。Landmark#GeneratePointMarkerの構成通りでないと動かない            
            return target.transform.GetChild(0).transform.position;
        }

        /// <summary>
        /// 見通し解析のラインを生成する(解析を行う)
        /// </summary>
        float CreateLineOfSight()
        {
            ClearLineOfSight();
            var landMarkPoint = GetTargetPos(targetLandmark);
            analyzeLandmarkData.startPosName = targetLandmark.name;
            analyzeLandmarkData.startPos = landMarkPoint;

            var obj = new GameObject(ObjNameLineOfSight);
            obj.transform.parent = targetLandmark.transform;

            int circumferenceInterval = LandmarkViewRayInterval * analyzeLandmarkData.raySpan;
            int radiusInterval = 5;
            int radius = analyzeLandmarkData.rangeRadius;
            int rangeUp = analyzeLandmarkData.rangeUp;
            int rangeDown = analyzeLandmarkData.rangeDown;
            float result = -1;
            Vector3 targetPoint = new Vector3(0, 0, 0);

            for (int i = 0; i < circumferenceInterval; i++)
            {
                // 上方向のラインの生成
                for (int j = 0; j < rangeUp; j++)
                {
                    targetPoint = landMarkPoint;
                    targetPoint.x += radius * Mathf.Cos(2 * Mathf.PI * i / (float)circumferenceInterval);
                    targetPoint.y += j;
                    targetPoint.z += radius * Mathf.Sin(2 * Mathf.PI * i / (float)circumferenceInterval);
                    RaycastHit hit;
                    if (RaycastBuildings(targetLandmark, targetPoint, out hit))
                    {
                        DrawLine(landMarkPoint, hit.point, obj, analyzeLandmarkData.lineColorValid);
                        DrawLine(hit.point, targetPoint, obj, analyzeLandmarkData.lineColorInvalid);
                    }
                    else
                    {
                        DrawLine(landMarkPoint, targetPoint, obj, analyzeLandmarkData.lineColorValid);
                    }
                }
                // 下方向のラインの生成
                for (int j = 0; j < rangeDown; j++)
                {
                    targetPoint = landMarkPoint;
                    targetPoint.x += radius * Mathf.Cos(2 * Mathf.PI * i / (float)circumferenceInterval);
                    targetPoint.y -= j;
                    targetPoint.z += radius * Mathf.Sin(2 * Mathf.PI * i / (float)circumferenceInterval);
                    RaycastHit hit;
                    if (RaycastBuildings(targetLandmark, targetPoint, out hit))
                    {
                        DrawLine(landMarkPoint, hit.point, obj, analyzeLandmarkData.lineColorValid);
                        DrawLine(hit.point, targetPoint, obj, analyzeLandmarkData.lineColorInvalid);
                    }
                    else
                    {
                        DrawLine(landMarkPoint, targetPoint, obj, analyzeLandmarkData.lineColorValid);
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
            if (targetLandmark == null)
            {
                return;
            }
            var root = targetLandmark.transform;
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

            var startPos = GetTargetPos(startPoint);

            hitInfo = new RaycastHit();

            Vector3 direction = (destination - startPos).normalized;

            RaycastHit[] hits;
            hits = Physics.RaycastAll(startPos, direction, analyzeLandmarkData.rangeRadius);

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
        /// 眺望対象解析のスライダーの登録
        /// </summary>
        public void SetAnalyzeRange()
        {
            var slider_Landmark = analyzeSettingPanel.Q<VisualElement>("Slider_Landmark");

            var distanceSlider = slider_Landmark.Q<SliderInt>("DistanceSlider");
            distanceSlider.lowValue = LandmarkViewMinDistance;
            distanceSlider.highValue = LandmarkViewMaxDistance;
            distanceSlider.value = analyzeLandmarkData.rangeRadius;
            distanceSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeLandmarkData.rangeRadius = evt.newValue;
                CreateLineOfSight();
            });


            var upperSlider = slider_Landmark.Q<SliderInt>("UpperSlider");
            upperSlider.lowValue = 0;
            upperSlider.highValue = 50;
            upperSlider.value = analyzeLandmarkData.rangeUp;
            upperSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeLandmarkData.rangeUp = evt.newValue;
                CreateLineOfSight();
            });

            var downSlider = slider_Landmark.Q<SliderInt>("DownSlider");
            downSlider.lowValue = 0;
            downSlider.highValue = 50;
            downSlider.value = analyzeLandmarkData.rangeDown;
            downSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeLandmarkData.rangeDown = evt.newValue;
                CreateLineOfSight();
            });

            var raySpanSlider = slider_Landmark.Q<SliderInt>("RaySpanSlider");
            raySpanSlider.lowValue = 1;
            raySpanSlider.highValue = 10;
            raySpanSlider.value = analyzeLandmarkData.raySpan;
            raySpanSlider.RegisterValueChangedCallback(evt =>
            {
                analyzeLandmarkData.raySpan = evt.newValue;
                CreateLineOfSight();
            });

            var visibleCheckbox = slider_Landmark.Q<Toggle>("validVisibleCheck");
            visibleCheckbox.value = visibleValid;
            visibleCheckbox.RegisterValueChangedCallback(evt =>
            {
                visibleValid = evt.newValue;
                SetValidAreaColor(visibleValid);
                CreateLineOfSight();
            });

            var invisibleCheckbox = slider_Landmark.Q<Toggle>("invalidVisibleCheck");
            invisibleCheckbox.value = visibleInvalid;
            invisibleCheckbox.RegisterValueChangedCallback(evt =>
            {
                visibleInvalid = evt.newValue;

                SetInvalidAreaColor(visibleInvalid);
                CreateLineOfSight();
            });


        }

        /// <summary>
        /// 眺望対象の解析結果を登録する
        /// </summary>
        public AnalyzeLandmarkElements RegisterAnalyzeData()
        {
            var keyName = CreateAnalyzeDataDictKey(analyzeLandmarkData);
            var isAdded = lineOfSightDataComponent.AddAnalyzeLandmarkData(keyName, analyzeLandmarkData);
            if (isAdded)
            {
                return analyzeLandmarkData;
            }
            else
            {
                return AnalyzeLandmarkElements.Empty;
            }
        }

        /// <summary>
        /// 眺望対象の解析結果を編集する
        /// </summary>
        public (string deleteButtonName, AnalyzeLandmarkElements analyzeLandmarkData) EditAnalyzeData()
        {
            var deleteButtonName = DeleteAnalyzeData();
            var keyName = CreateAnalyzeDataDictKey(analyzeLandmarkData);
            var isAdded = lineOfSightDataComponent.AddAnalyzeLandmarkData(keyName, analyzeLandmarkData);
            if (isAdded)
            {
                return (deleteButtonName, analyzeLandmarkData);
            }
            else
            {
                return ("", AnalyzeLandmarkElements.Empty);
            }
        }

        /// <summary>
        /// 眺望対象の解析結果を削除する
        /// </summary>
        public string DeleteAnalyzeData()
        {
            ClearLineOfSight();
            if (editLandmarkData.Equals(AnalyzeLandmarkElements.Empty))
            {
                return "";
            }
            var keyName = CreateAnalyzeDataDictKey(editLandmarkData);
            var isRemoved = lineOfSightDataComponent.RemoveAnalyzeLandmarkData(keyName);
            editLandmarkData = AnalyzeLandmarkElements.Empty;
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
        /// 登録キーの生成
        /// </summary>
        private string CreateAnalyzeDataDictKey(AnalyzeLandmarkElements analyzeLandmarkElements)
        {
            var key = analyzeLandmarkElements.startPosName + analyzeLandmarkElements.rangeUp + analyzeLandmarkElements.rangeDown;
            return key;
        }


        public bool SetTargetLandmark(string targetName)
        {
            var landmarkMarkers = GameObject.Find("LandmarkMarkers");
            var prevTarget = targetLandmark;
            foreach (Transform child in landmarkMarkers.transform)
            {
                if (child.gameObject.name != targetName)
                {
                    continue;
                }

                targetLandmark = child.gameObject;
                break;
            }

            if (targetLandmark == null)
            {
                // targetLandmarkが設定されていない場合はfalse
                return false;
            }

            // 同じターゲットの場合は指定した物が設定されていないのでfalseとみなす
            return targetLandmark != prevTarget;

        }

        /// <summary>
        /// 解析結果のボタンが押されたときの処理
        /// </summary>
        public void ButtonAction(AnalyzeLandmarkElements analyzeLandmarkElements)
        {
            editLandmarkData = analyzeLandmarkElements;
            analyzeLandmarkData = analyzeLandmarkElements;
            SetTargetLandmark(analyzeLandmarkData.startPosName);
            // var landmarkMarkers = GameObject.Find("LandmarkMarkers");
            // foreach (Transform child in landmarkMarkers.transform)
            // {
            //     if (child.gameObject.name == analyzeLandmarkData.startPosName)
            //     {
            //         targetLandmark = child.gameObject;
            //         break;
            //     }
            // }
            if (targetLandmark == null)
            {
                Debug.LogWarning($"cannot find targetLandmark : {analyzeLandmarkData.startPosName}");
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
                .AnalyzeLandmarkDatas.Find(point => point.Name == name);
            return data.IsProject(ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID);
        }

        public override void OnSelect()
        {
            SetTarget();
        }

        public override void OnDisable()
        {
        }
    }
}
