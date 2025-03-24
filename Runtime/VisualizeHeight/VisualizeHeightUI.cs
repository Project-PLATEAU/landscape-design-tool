using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Landscape2.Runtime.UiCommon;
using System.ComponentModel;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 高さ可視化機能のUI
    /// </summary>
    public class VisualizeHeightUI : ISubComponent
    {
        private readonly LandscapeCamera landscapeCamera;
        private readonly VisualizeHeight visualizeHeight;
        private VisualElement visualizeHeightPanel;    // 高さ可視化Panel全体
        private VisualElement pointOfViewPanel; // 俯瞰モード用Panel
        private VisualElement walkerPanel; // 歩行者モード用Panel
        private readonly VisualTreeAsset visualizeHeightUXML; // 高さピンのHUD用uxml

        // GlobalNavi_Main用
        private readonly Toggle heightToggle; // 高さ可視化トグル
        private const string UIHeightToggle = "Toggle_HeightDisplay"; // 高さ可視化トグル名前
        private const float headerOffset = 145.0f; // ヘッダーパネルの高さのオフセット
        private SliderInt heightSlider; // 可視化下限スライダー
        private const string UIHeightSlider = "HeightSlider"; // 可視化下限スライダー名前

        // HeightDisplay用
        private VisualElement heightPinClone; // 高さピンのクローン
        private VisualElement walkerPin; // 歩行者モード用高さピン
        private const string UIHeightPin = "HeightPin"; // 高さピン名前
        private const float pinOffsetx = 40.0f; // 高さピンのオフセット(width/2)
        private const float pinOffsety = 125.0f; // 高さピンのオフセット(height)

        // 建物と高さピンの対応リスト
        private List<(PLATEAUCityObjectGroup Building, VisualElement Pin)> bldgList = new List<(PLATEAUCityObjectGroup, VisualElement)>();

        private bool isCameraMoved = false;
        private const float cameraDistance = 500f;
        private PLATEAUCityObjectGroup selectedBuilding;
        private int lastPinID = 0;
        private GameObject highlightBox = null;

        public VisualizeHeightUI(VisualizeHeight visualizeHeight, VisualElement uiRoot, LandscapeCamera landscapeCamera)
        {
            this.visualizeHeight = visualizeHeight;
            this.landscapeCamera = landscapeCamera;
            landscapeCamera.OnSetCameraCalled += HandleSetCameraCalled;

            // 高さ可視化用のUXMLを生成
            visualizeHeightUXML = Resources.Load<VisualTreeAsset>("HeightHUD");

            visualizeHeightPanel = new UIDocumentFactory().CreateWithUxmlName("HeightDisplay");
            visualizeHeightPanel.style.display = DisplayStyle.Flex;
            pointOfViewPanel = visualizeHeightPanel.Q<VisualElement>("PointOfViewDisplay");
            walkerPanel = visualizeHeightPanel.Q<VisualElement>("WalkerViewDisplay");
            walkerPanel.RegisterCallback<MouseDownEvent>(evt => OnPanelClick());
            walkerPanel.style.display = DisplayStyle.None;
            heightToggle = uiRoot.Q<Toggle>(UIHeightToggle);
            heightSlider = uiRoot.Q<SliderInt>(UIHeightSlider);

            // uxmlのSortOrderを設定
            GameObject.Find("HeightDisplay").GetComponent<UIDocument>().sortingOrder = -1;

            // 可視化下限スライダーの初期設定
            heightSlider.style.display = DisplayStyle.None;
            heightSlider.value = 10; // 初期値は10m

            heightSlider.RegisterValueChangedCallback((evt) =>
            {
                UpdateHeightPinsDisplay(evt.newValue);
            });

            SetHeightPin();

            // 高さ可視化トグルのイベント登録
            heightToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue == true)
                {
                    heightSlider.style.display = DisplayStyle.Flex;
                    pointOfViewPanel.style.display = DisplayStyle.Flex;

                    for (int i = 0; i <= lastPinID; i++)
                    {
                        bldgList[i].Pin.style.translate = UpdateHeightPinPosition(bldgList[i].Building);
                    }
                }
                else
                {
                    heightSlider.style.display = DisplayStyle.None;
                    pointOfViewPanel.style.display = DisplayStyle.None;
                }
            });
        }

        /// <summary>
        /// カメラの状態が変更されたら呼び出される関数
        /// </summary>
        private void HandleSetCameraCalled()
        {
            var cameraState = landscapeCamera.cameraState;

            // 歩行者モードまたは歩行者視点選択モードの場合
            if (cameraState != LandscapeCameraState.PointOfView)
            {
                heightToggle.style.display = DisplayStyle.None;
                walkerPanel.style.display = DisplayStyle.Flex;

                // 俯瞰モードにおける高さ可視化をOFFにする
                heightToggle.value = false;
            }
            else if (cameraState == LandscapeCameraState.PointOfView)
            {
                heightToggle.style.display = DisplayStyle.Flex;
                walkerPanel.style.display = DisplayStyle.None;
                walkerPin.style.display = DisplayStyle.None;
                selectedBuilding = null;
                if (highlightBox != null) GameObject.Destroy(highlightBox);
            }
        }

        /// <summary>
        /// 高さピンの初期化
        /// </summary>
        private void SetHeightPin()
        {
            // すべての建物を取得
            List<PLATEAUCityObjectGroup> buildingList = new List<PLATEAUCityObjectGroup>();
            buildingList = visualizeHeight.GetBuildingList();

            foreach (var building in buildingList)
            {
                heightPinClone = visualizeHeightUXML.CloneTree().Q<VisualElement>(UIHeightPin);
                bldgList.Add((building, heightPinClone));

                // 高さピンを複製
                pointOfViewPanel.Add(heightPinClone);

                // 建物の高さを設定
                string height = visualizeHeight.GetBuildingHeight(building);
                if (string.IsNullOrEmpty(height))
                {
                    height = "---";
                }
                bldgList[bldgList.Count - 1].Pin.Q<Label>().text = height;
                bldgList[bldgList.Count - 1].Pin.style.display = DisplayStyle.None;

                //高さピンをスクリーン外に配置
                bldgList[bldgList.Count - 1].Pin.style.translate = new Translate() { x = -1000, y = -1000 };
            }
            // 建物が高い順にソート
            bldgList = bldgList.OrderByDescending(item => float.Parse(item.Pin.Q<Label>().text)).ToList();

            // 歩行者モードの高さピンを初期化
            if (heightPinClone == null)
            {
                // buildingList.Count() <= 0の時
                heightPinClone = visualizeHeightUXML.CloneTree().Q<VisualElement>(UIHeightPin);
            }
            walkerPanel.Add(heightPinClone);
            walkerPin = walkerPanel.Q<VisualElement>(UIHeightPin);
            walkerPin.style.display = DisplayStyle.None;

            UpdateHeightPinsDisplay(heightSlider.value);
        }

        /// <summary>
        /// 高さ上限に応じて表示されるピンを変更する
        /// </summary>
        private void UpdateHeightPinsDisplay(float heightLimit)
        {
            if (bldgList.Count() <= 0)
            {
                Debug.LogWarning($"bldgList.Count() <= 0");
                return;
            }
            // 高さピンのリストを初期化
            var pin = bldgList[lastPinID].Pin;
            float height = float.Parse(pin.Q<Label>().text);
            if (heightLimit <= height)
            {
                while (heightLimit <= float.Parse(bldgList[lastPinID].Pin.Q<Label>().text))
                {
                    bldgList[lastPinID].Pin.style.display = DisplayStyle.Flex;
                    if (lastPinID < bldgList.Count - 1)
                    {
                        lastPinID++;
                    }
                    else break;
                }
            }
            else
            {
                while (heightLimit > float.Parse(bldgList[lastPinID].Pin.Q<Label>().text))
                {
                    bldgList[lastPinID].Pin.style.display = DisplayStyle.None;
                    if (lastPinID > 0)
                    {
                        lastPinID--;
                    }
                    else break;
                }
            }
        }

        /// <summary>
        /// 高さピンの位置を更新
        /// </summary>
        private Translate UpdateHeightPinPosition(PLATEAUCityObjectGroup building)
        {
            // 建物のTransform.positionをScreen座標に変換
            var bldgBounds = building.transform.GetComponent<MeshCollider>().bounds;

            var topPos = new Vector3(bldgBounds.center.x, bldgBounds.max.y, bldgBounds.center.z);
            var screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(visualizeHeightPanel.panel, topPos, Camera.main);

            var vp = Camera.main.WorldToViewportPoint(topPos);
            bool isActive = vp.x >= 0.0f && vp.x <= 1.0f && vp.y >= 0.0f && vp.y <= 1.0f && vp.z >= 0.0f;
            bool isInScreen = screenPos.x > pinOffsetx && screenPos.x < Screen.width - pinOffsetx && screenPos.y > headerOffset + pinOffsety;
            bool isNearCamera = Vector2.Distance(new Vector2(topPos.x, topPos.z), new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z)) < cameraDistance;

            // ヘッダーパネルをのぞくScreenに映る範囲内の場合は表示
            if (isActive && isInScreen && isNearCamera)
            {
                //高さピンの位置を設定
                var xPos = screenPos.x - pinOffsetx;
                var yPos = screenPos.y - pinOffsety;
                return new Translate() { x = xPos, y = yPos };
            }
            else
            {
                //高さピンをスクリーン外に配置
                return new Translate() { x = -1000, y = -1000 };
            }
        }

        /// <summary>
        /// 歩行者モードにおいて高さ可視化パネルがクリックされた場合の処理
        /// </summary>
        private void OnPanelClick()
        {
            if (landscapeCamera.cameraState != LandscapeCameraState.Walker)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {
                // 建築物をクリックした場合
                if (hit.collider.gameObject.name.Contains("bldg_"))
                {
                    var targetObject = hit.collider.gameObject;
                    // 選択した建物の高さを表示
                    OnBuildingSelected(targetObject);
                }
            }
        }

        /// <summary>
        /// 歩行者モードにおいて建物が選択された場合の処理
        /// </summary>
        public void OnBuildingSelected(GameObject targetObject)
        {
            var building = targetObject.GetComponent<PLATEAUCityObjectGroup>();
            if (building == null) return;

            if (building != selectedBuilding)
            {
                // 高さを更新
                walkerPin.Q<Label>().text = bldgList.FirstOrDefault(item => item.Building == building).Pin.Q<Label>().text;
                // 位置を更新
                walkerPin.style.display = DisplayStyle.Flex;
                walkerPin.style.translate = UpdateHeightPinPosition(building);
                selectedBuilding = building;
                CreateHighlightBox(targetObject);
            }
            else // 同じ建物をクリックした場合
            {
                walkerPin.style.display = DisplayStyle.None;
                selectedBuilding = null;
                GameObject.Destroy(highlightBox);
            }
        }

        /// <summary>
        /// 建物選択時のハイライトボックスを生成する
        /// </summary>
        private void CreateHighlightBox(GameObject targetObject)
        {
            if (highlightBox == null)
            {
                var bbox = Resources.Load("bbox") as GameObject;
                highlightBox = GameObject.Instantiate(bbox);

                MeshFilter mf = highlightBox.GetComponent<MeshFilter>();
                mf.mesh.SetIndices(mf.mesh.GetIndices(0), MeshTopology.LineStrip, 0);
            }

            var meshColider = targetObject.GetComponent<MeshCollider>();
            var bounds = meshColider.bounds;

            highlightBox.transform.localPosition = bounds.center;
            highlightBox.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
        }

        /// <summary>
        /// 視点が変更された場合の処理
        /// </summary>
        private void UpdateHeightView()
        {
            // 視点が変更された場合は高さピンを更新
            if (Camera.main.transform.hasChanged)
            {
                if (landscapeCamera.cameraState == LandscapeCameraState.Walker) // 歩行者モードの場合
                {
                    walkerPin.style.display = DisplayStyle.None;
                }
                else
                {
                    pointOfViewPanel.style.display = DisplayStyle.None;
                }
                // 視点が変更されたフラグを立てる
                isCameraMoved = true;
                Camera.main.transform.hasChanged = false;
            }
            else
            {
                if (isCameraMoved == true)
                {
                    isCameraMoved = false;

                    // ピンの位置を更新
                    if (landscapeCamera.cameraState == LandscapeCameraState.Walker) // 歩行者モードの場合
                    {
                        if (selectedBuilding != null)
                        {
                            walkerPin.style.display = DisplayStyle.Flex;
                            walkerPin.style.translate = UpdateHeightPinPosition(selectedBuilding);
                        }
                    }
                    else if (heightToggle.value == true) // 俯瞰モードの場合
                    {
                        pointOfViewPanel.style.display = DisplayStyle.Flex;
                        for (int i = 0; i <= lastPinID; i++)
                        {
                            bldgList[i].Pin.style.translate = UpdateHeightPinPosition(bldgList[i].Building);
                        }
                    }
                }
            }
        }

        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
            // 高さピンの表示を更新
            UpdateHeightView();
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
