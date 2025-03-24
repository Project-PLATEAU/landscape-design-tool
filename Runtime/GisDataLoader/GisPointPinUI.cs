using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// ピン表示用のモデル
    /// </summary>
    public class GisPointPinUI
    {
        private GisPointPin pin;

        private Vector3 facilityPosition;
        
        private const int pinOffsetx = 46 / 2;
        private const int pinOffsety = 90;
        
        // テキストを表示するカメラ位置
        private const int textDisplayCameraDepth = 800;

        public GisPointPinUI(GisPointInfo info, string attributeID, VisualElement element)
        {
            pin = new GisPointPin(info.ID, attributeID, element);
            
            // ピンをabsoluteに設定して被っても問題ないようにする
            element.style.position = Position.Absolute;
            
            // ピンの名前を設定
            element.Q<Label>("Icon_Name").text = info.FacilityName;
            
            // ピンの色を設定
            var color = element.Q<VisualElement>("Icon_Pin");
            color.style.unityBackgroundImageTintColor = info.Color;
            
            // 施設の位置を保持
            facilityPosition = info.FacilityPosition;
        }

        public int GetID()
        {
            return pin.PointID;
        }

        public VisualElement GetElement()
        {
            return pin.Element;
        }

        public bool IsSameAttribute(string attributeID)
        {
            return pin.AttributeID == attributeID;
        }

        public void Show(bool isShow)
        {
            pin.Element.Q<VisualElement>("Pin").style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowText(bool isShow)
        {
            pin.Element.Q<Label>("Icon_Name").style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetScreen(Vector2 position, bool isShow)
        {
            if (position == Vector2.zero || !isShow)
            {
                Show(false);
                return;
            }
            Show(true);
            
            // ピンのサイズを考慮して位置を調整
            var xPos = position.x - pinOffsetx;
            var yPos = position.y - pinOffsety;
            pin.Element.style.translate = new Translate() { x = xPos, y = yPos };
            
            var vp = Camera.main.WorldToViewportPoint(facilityPosition);
            
            // カメラが離れすぎたら、テキストを非表示にする
            ShowText(vp.z <= textDisplayCameraDepth);
        }
    }
}