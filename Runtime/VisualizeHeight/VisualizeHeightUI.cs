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
        private List<VisualHeightPin> bldgList = new List<VisualHeightPin>();

        private bool isCameraMoved = false;
        private const float cameraDistance = 500f;
        private string selectedBuildingGmlID;
        private Bounds? selectedBuildingBounds = null;
        private float heightLimit = 10; // 初期値は10
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
            heightSlider.value = (int)heightLimit;

            heightSlider.RegisterValueChangedCallback((evt) =>
            {
                heightLimit = evt.newValue;
                // ピンを再取得・表示
                UpdateCurrentPointViewPins();
            });

            // 高さ可視化トグルのイベント登録
            heightToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue == true)
                {
                    heightSlider.style.display = DisplayStyle.Flex;
                    pointOfViewPanel.style.display = DisplayStyle.Flex;

                    // ピンを再取得・表示
                    UpdateCurrentPointViewPins();
                    
                    // 新しいピンの設定
                    CreatePointViewPins();
                }
                else
                {
                    heightSlider.style.display = DisplayStyle.None;
                    pointOfViewPanel.style.display = DisplayStyle.None;
                }
            });
            
            // 歩行者ピンの初期化
            InitializeWalkerPin();
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
                selectedBuildingBounds = null;
                selectedBuildingGmlID = null;
                if (highlightBox != null) GameObject.Destroy(highlightBox);
            }
        }

        /// <summary>
        /// 俯瞰表示の高さピンの生成
        /// </summary>
        private void CreatePointViewPins()
        {
            // 既に追跡済みの GML ID を取得
            var seenGmlIds = bldgList.Select(b => b.GmlID).ToHashSet();

            // 新しい建物を取得
            var newBuildingsInView = visualizeHeight.GetBuildingListInCameraView(Camera.main, seenGmlIds, cameraDistance);
            foreach (var building in newBuildingsInView)
            {
                string gmlId = visualizeHeight.GetBuildingGmlId(building);

                // データ抽出
                var meshCollider = building.GetComponent<MeshCollider>();
                if (meshCollider == null) continue;

                string heightStr = visualizeHeight.GetBuildingHeight(building);
                if (string.IsNullOrEmpty(heightStr)) continue;

                float height = float.Parse(heightStr);
                Bounds bounds = meshCollider.bounds;

                // ピン UI 作成
                var pinVisualElement = visualizeHeightUXML.CloneTree().Q<VisualElement>(UIHeightPin);
                pointOfViewPanel.Add(pinVisualElement);
                pinVisualElement.Q<Label>().text = heightStr;
                
                // ピンとして登録
                var pin = new VisualHeightPin
                {
                    GmlID = gmlId,
                    Bounds = bounds,
                    Height = height,
                    Pin = pinVisualElement
                };
                
                // 表示を設定
                pin.SetDisplay(heightLimit);
                if (pin.IsVisible())
                {
                    pinVisualElement.style.translate = UpdateHeightPinPosition(bounds);
                }

                // VisualHeightPin として追加
                bldgList.Add(pin);
            }
        }

        /// <summary>
        /// ピンの位置と表示を更新
        /// </summary>
        private void UpdateCurrentPointViewPins()
        {
            // ピンの位置と表示を更新
            foreach (var visualPin in bldgList)
            {
                visualPin.SetDisplay(heightLimit);
                if (visualPin.IsVisible())
                {
                    visualPin.Pin.style.translate = UpdateHeightPinPosition(visualPin.Bounds);
                }
            }
        }

        /// <summary>
        /// 歩行者モードのピンを初期化
        /// </summary>
        private void InitializeWalkerPin()
        {
            // 歩行者モードの高さピンを初期化
            if (heightPinClone == null)
            {
                // buildingList.Count() <= 0の時
                heightPinClone = visualizeHeightUXML.CloneTree().Q<VisualElement>(UIHeightPin);
            }
            walkerPanel.Add(heightPinClone);
            walkerPin = walkerPanel.Q<VisualElement>(UIHeightPin);
            walkerPin.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 高さピンの位置を更新
        /// </summary>
        private Translate UpdateHeightPinPosition(Bounds bounds)
        {
            var topPos = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
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

            string gmlId = visualizeHeight.GetBuildingGmlId(building);

            if (gmlId != selectedBuildingGmlID)
            {
                walkerPin.Q<Label>().text = visualizeHeight.GetBuildingHeight(building);

                var meshCollider = building.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    selectedBuildingBounds = meshCollider.bounds;
                    selectedBuildingGmlID = gmlId;

                    if (selectedBuildingBounds != null)
                    {
                        walkerPin.style.display = DisplayStyle.Flex;
                        walkerPin.style.translate = UpdateHeightPinPosition(selectedBuildingBounds.Value);

                        CreateHighlightBox(targetObject);
                    }
                }
            }
            else // 同じ建物をクリックした場合
            {
                walkerPin.style.display = DisplayStyle.None;
                selectedBuildingBounds = null;
                selectedBuildingGmlID = null;
                if (highlightBox != null) GameObject.Destroy(highlightBox);
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
                        if (selectedBuildingBounds.HasValue)
                        {
                            walkerPin.style.display = DisplayStyle.Flex;
                            walkerPin.style.translate = UpdateHeightPinPosition(selectedBuildingBounds.Value);
                        }
                    }
                    else if (heightToggle.value == true) // 俯瞰モードの場合
                    {
                        pointOfViewPanel.style.display = DisplayStyle.Flex;

                        // ピンを再取得・表示
                        UpdateCurrentPointViewPins();
                        
                        // 新しいピンを生成
                        CreatePointViewPins();
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
