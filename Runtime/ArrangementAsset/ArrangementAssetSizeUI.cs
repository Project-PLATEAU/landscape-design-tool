using Landscape2.Runtime.UiCommon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ArrangementAssetSizeUI : ISubComponent
    {
        const string WidthLabelName = "Width";
        const string HeightLabelName = "Height";
        const string DepthLabelName = "Depth";

        const string NothingParamValue = "---";

        VisualElement element;

        GameObject targetObj;

        Label widthLabel;
        Label heightLabel;
        Label depthLabel;

        public ArrangementAssetSizeUI()
        {
            element = new UIDocumentFactory().CreateWithUxmlName("AssetSizeHUD");

            widthLabel = element.Q<Label>(WidthLabelName); // 横
            heightLabel = element.Q<Label>(HeightLabelName); // 縦
            depthLabel = element.Q<Label>(DepthLabelName); // 高さ

            UpdateLabel(null);
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

        public void Update(float deltaTime)
        {
            if (targetObj == null)
            {
                return;
            }
            CalcUIDisplayPosition(targetObj);
            var size = GetGameObjectSize(targetObj);
            UpdateLabel(targetObj != null ? size : null);
        }

        public void Start()
        {
        }

        void UpdateLabel(Vector3? size)
        {

            if (size == null)
            {
                widthLabel.text = NothingParamValue;
                heightLabel.text = NothingParamValue;
                depthLabel.text = NothingParamValue;
            }
            else
            {
                widthLabel.text = $"{size.Value.x:F3}";
                heightLabel.text = $"{size.Value.y:F3}";
                depthLabel.text = $"{size.Value.z:F3}";
            }
        }

        void CalcUIDisplayPosition(GameObject obj)
        {
            // オブジェクトの Bounds を取得
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"Renderer が見つかりません: {obj.name}");
                return;
            }
            var bounds = renderer.bounds;

            // ワールド空間のポイントを設定 (バウンディングボックスの上部中央)
            var wp = obj.transform.position; // new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            // スクリーン座標に変換
            var screenPos = Camera.main.WorldToScreenPoint(wp);

            // ビューポート座標に変換
            var vp = Camera.main.WorldToViewportPoint(wp);
            bool isActive = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            // UI 要素の範囲を確認
            if (!isActive)
            {
                return;
            }

            // UI 要素の中心を調整
            var elementWidth = element.resolvedStyle.width;
            var xcenter = elementWidth / 2f;

            // UI の位置を設定
            element.style.translate = new Translate(screenPos.x, Screen.height - screenPos.y);
        }


        public void Show(bool state)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;

        }

        public void SetTarget(GameObject target)
        {
            targetObj = target;
        }


        public Vector3 GetGameObjectSize(GameObject gameObject)
        {

            if (gameObject == null)
            {
                Debug.LogWarning("GameObject が null です。");
                return Vector3.zero;
            }

            // GameObject 内のすべての Renderer を取得
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning("Renderer が見つかりません。");
                return Vector3.zero;
            }

            // 初期化：最初の Renderer の Bounds を基準にする
            Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool initialized = false;

            foreach (var renderer in renderers)
            {
                // 各 Renderer の Bounds を取得
                Bounds localBounds = renderer.bounds;

                // ワールド座標系でのスケールを適用
                Vector3 worldMin = renderer.transform.TransformPoint(localBounds.min);
                Vector3 worldMax = renderer.transform.TransformPoint(localBounds.max);

                // スケール適用後の Bounds を作成
                Bounds worldBounds = new Bounds();
                worldBounds.SetMinMax(worldMin, worldMax);

                // 統合
                if (!initialized)
                {
                    combinedBounds = worldBounds;
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(worldBounds);
                }
            }

            // 統合された Bounds のサイズを返す
            return combinedBounds.size;
        }
    }
}
