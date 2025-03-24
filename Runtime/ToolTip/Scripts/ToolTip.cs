using Landscape2.Runtime.UiCommon;
using PLATEAU.CityGML;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


namespace Landscape2.Runtime
{
    public class ToolTip : ISubComponent
    {
        private VisualElement rootElement;

        private VisualElement toolTip;
        private Label description;

        private bool widthFixed = false;

        public bool IsVisible => toolTip.visible;

        public ToolTip(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            rootElement = root.panel != null ? root.panel.visualTree : root;

            toolTip = new UIDocumentFactory().CreateWithUxmlName("Tooltip_Up");
            toolTip.style.position = Position.Absolute;
            description = toolTip.Q<Label>("Description");

            root.Add(toolTip);

            ShowTip(false);
            toolTip.RegisterCallback<GeometryChangedEvent>(GeometryChangeCallback);
        }

        void GeometryChangeCallback(GeometryChangedEvent evt)
        {
            if (widthFixed)
            {
                return;
            }
            var width = evt.newRect.width;

            toolTip.style.width = width;
            toolTip.style.minWidth = width;
            toolTip.style.maxWidth = width;

            widthFixed = true;
            toolTip.UnregisterCallback<GeometryChangedEvent>(GeometryChangeCallback);
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
            // マウスのスクリーン座標（原点：左下）を取得し、UIToolkit用にY軸反転
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            // 取得したいVisualElementを格納するリスト
            List<VisualElement> hitElements = new List<VisualElement>();

            // ルートから再帰的に探索して、マウス位置に含まれる要素を収集する
            GetElementsAtPosition(rootElement, mousePos, hitElements, 100);

            var tipElement = hitElements.Where(x => !string.IsNullOrEmpty(x.tooltip)).FirstOrDefault();
            if (tipElement != null)
            {
                SetTipValueFromElement(tipElement);
                if (!IsVisible)
                {
                    ShowTip(true);
                }
            }
            else
            {
                if (IsVisible)
                {
                    ShowTip(false);
                }
            }
        }

        /// <summary>
        /// 再帰的にVisualElementの階層を走査し、worldBoundが指定の位置を含む要素をリストに追加する
        /// </summary>
        /// <param name="element">走査対象のVisualElement</param>
        /// <param name="pos">パネル座標系での位置</param>
        /// <param name="results">結果を追加するリスト</param>
        private void GetElementsAtPosition(VisualElement element, Vector2 pos, List<VisualElement> results, int recursiveCount)
        {
            bool contains = element.worldBound.Contains(pos);
            if (recursiveCount <= 0 || !contains)
            {
                return;
            }
            // 要素がパネルにアタッチされている場合のみ判定
            if (element.panel != null && contains)
            {
                results.Add(element);
            }

            // 子要素も同様にチェック
            foreach (var child in element.Children())
            {
                GetElementsAtPosition(child, pos, results, recursiveCount--);
            }
        }

        private void ShowTip(bool show)
        {
            toolTip.visible = show;
        }

        private void SetTipValueFromElement(VisualElement tipElement)
        {
            var tipPos = rootElement.WorldToLocal(tipElement.worldBound.center);
            tipPos.y += tipElement.resolvedStyle.height / 2;
            SetTipValueFromText(tipElement.tooltip, tipPos);
        }

        private void SetTipValueFromText(string label, Vector2 pos)
        {
            description.text = label;
            SetTipPos(pos);
        }

        private void SetTipPos(Vector2 pos)
        {
            toolTip.style.left = pos.x - (toolTip.resolvedStyle.width / 2);
            toolTip.style.top = pos.y;
        }
    }
}
