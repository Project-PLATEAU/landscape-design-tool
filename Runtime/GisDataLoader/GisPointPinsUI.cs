using Landscape2.Runtime.UiCommon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// マップにポイントを表示するプレゼンター
    /// </summary>
    public class GisPointPinsUI
    {
        private VisualTreeAsset pinVisualTreeAsset;
        private VisualElement pinPanel;
        private GisPointInfos gisPointInfos;
        
        // 表示中のピン情報
        private readonly List<GisPointPinUI> pins = new();
        
        private string GetID(int attributeIndex, int index) => $"GIS_{attributeIndex}_{index}";
        
        public GisPointPinsUI(GisPointInfos gisPointInfos)
        {
            this.gisPointInfos = gisPointInfos;
            pinVisualTreeAsset = Resources.Load<VisualTreeAsset>("Pin");
            pinPanel = new UIDocumentFactory().CreateWithUxmlName("Pin");
            if (GameObject.Find("Pin").TryGetComponent<UIDocument>(out var uiDocument))
            {
                // ピンがUIの裏側になるようにsorting order 設定
                uiDocument.sortingOrder = -1;
            }
            
            pinPanel.Q<VisualElement>("Pin").style.display = DisplayStyle.None;
            
            RegisterEvents();
        }
        
        private void RegisterEvents()
        {
            gisPointInfos.OnCreate.AddListener(CreatePin);
            gisPointInfos.OnUpdateDisplay.AddListener(UpdateDisplayPin);
            gisPointInfos.OnDelete.AddListener(DeletePin);
            gisPointInfos.OnDeleteAll.AddListener(DeleteAll);
        }
        
        private void CreatePin(string attributeID)
        {
            // GISのデータを取得
            var pointInfos = gisPointInfos.GetAttributeAll(attributeID);
            
            foreach (var gisPointInfo in pointInfos)
            {
                var pinElement = pinVisualTreeAsset.CloneTree();
                var pin = new GisPointPinUI(gisPointInfo, attributeID, pinElement);

                // ピンの位置更新
                var screenPos = GetPinScreenPosition(gisPointInfo.FacilityPosition);
                pin.SetScreen(screenPos, gisPointInfo.IsShow);
                
                // ピンのElementを追加
                pinPanel.Add(pinElement);

                // ピンのリストに追加
                pins.Add(pin);
            }
        }
        
        private void UpdateDisplayPin(string attributeID, bool isShow)
        {
            var pinList = pins.FindAll(pin => pin.IsSameAttribute(attributeID));
            if (pinList.Count <= 0)
            {
                Debug.LogWarning("ピンが見つかりません");
                return;
            }
            
            // 表示切り替え
            foreach (var pin in pinList)
            {
                var info = gisPointInfos.Get(pin.GetID());
                var pos = GetPinScreenPosition(info.FacilityPosition);
                if (pos != Vector2.zero)
                {
                    pin.Show(isShow);
                }
            }
        }
        
        private void DeletePin(string attributeID)
        {
            var pinList = pins.FindAll(pin => pin.IsSameAttribute(attributeID));
            if (pinList.Count < 0)
            {
                Debug.LogWarning("ピンが見つかりません");
                return;
            }

            // 削除
            foreach (var pin in pinList)
            {
                pinPanel.Remove(pin.GetElement());
                pins.Remove(pin);
            }
        }
        
        private void DeleteAll(List<string> deleteAttributeIDs)
        {
            foreach (var deleteAttributeID in deleteAttributeIDs)
            {
                DeletePin(deleteAttributeID);
            }
        }
        
        private Vector2 GetPinScreenPosition(Vector3 position)
        {
            // 施設の場所をスクリーン上で取得
            var screenPos = RuntimePanelUtils.CameraTransformWorldToPanel(pinPanel.panel, position, Camera.main);
            
            // ビューポート座標から、画面内に表示されているか判定
            var vp = Camera.main.WorldToViewportPoint(position);
            bool isScreen = (vp.x is >= 0.0f and <= 1.0f) && (vp.y is >= 0.0f and <= 1.0f) && vp.z >= 0.0f;
            
            // ヘッダーの考慮
            const float headerOffset = 145;
            isScreen &= screenPos.y >= headerOffset;
                
            if (!isScreen)
            {
                return Vector2.zero;
            }
            return screenPos;
        }

        public void OnUpdate()
        {
            foreach (var pin in pins)
            {
                var info = gisPointInfos.Get(pin.GetID());
                if (info == null)
                {
                    continue;
                }
                var screenPos = GetPinScreenPosition(info.FacilityPosition);
                pin.SetScreen(screenPos, info.IsShow);
            }
        }
    }
}