using Landscape2.Runtime.UiCommon;
using PLATEAU.Util;
using PlateauToolkit.Sandbox.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using PlateauSandboxBuilding = PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime.PlateauSandboxBuilding;

namespace Landscape2.Runtime
{
    public class ArrangementAssetSizeUI : ISubComponent
    {
        private const string WidthFieldName = "Width";
        private const string HeightFieldName = "Height";
        private const string DepthFieldName = "Depth";

        const string NothingParamValue = "---";

        private const float ButtonStep = 0.01f;
        private const float MinValue = 0.0f;
        private const float MaxValue = 1000f;


        private GameObject targetObj;

        private readonly VisualElement element;
        private readonly VisualElement column1;
        private readonly VisualElement column2;
        private readonly DoubleField widthField;
        private readonly DoubleField heightField;
        private readonly DoubleField depthField;
        private readonly DoubleField adWidthField;
        private readonly DoubleField adHeightField;
        private readonly Button widthUpButton;
        private readonly Button widthDownButton;
        private readonly Button heightUpButton;
        private readonly Button heightDownButton;
        private readonly Button depthUpButton;
        private readonly Button depthDownButton;
        private readonly Button adWidthUpButton;
        private readonly Button adWidthDownButton;
        private readonly Button adHeightUpButton;
        private readonly Button adHeightDownButton;

        private readonly AdAssetSizeEditor adAssetSizeEditor = new();

        public ArrangementAssetSizeUI()
        {
            element = new UIDocumentFactory().CreateWithUxmlName("AssetSizeHUD");

            column1 = element.Q<VisualElement>("1column");
            column2 = element.Q<VisualElement>("2column");
            widthField = element.Q<DoubleField>(WidthFieldName); // 横
            heightField = element.Q<DoubleField>(HeightFieldName); // 縦
            depthField = element.Q<DoubleField>(DepthFieldName); // 高さ
            adWidthField = column2.Q<DoubleField>("Width"); // 広告横
            adHeightField = column2.Q<DoubleField>("Height"); // 広告縦
            var widthArea = column1.Q<VisualElement>("Width-Area");
            widthUpButton = widthArea.Q<Button>("UpButton");
            widthDownButton = widthArea.Q<Button>("DownButton");
            var heightArea = column1.Q<VisualElement>("Height-Area");
            heightUpButton = heightArea.Q<Button>("UpButton");
            heightDownButton = heightArea.Q<Button>("DownButton");
            var depthArea = column1.Q<VisualElement>("Depth-Area");
            depthUpButton = depthArea.Q<Button>("UpButton");
            depthDownButton = depthArea.Q<Button>("DownButton");
            var adplaneArea = column2.Q<VisualElement>("Adplane-Area");
            adWidthUpButton = adplaneArea.Q<Button>("UpButton");
            adWidthDownButton = adplaneArea.Q<Button>("DownButton");
            var adHeightArea = column2.Q<VisualElement>("Height-Area");
            adHeightUpButton = adHeightArea.Q<Button>("UpButton");
            adHeightDownButton = adHeightArea.Q<Button>("DownButton");

            void InitField(DoubleField field)
            {
                field.formatString = "F2";
                field.isDelayed = true;
            }

            InitField(widthField);
            InitField(heightField);
            InitField(depthField);
            InitField(adWidthField);
            InitField(adHeightField);
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnDisable()
        {
            UnregisterCallbacks();
        }

        public void OnEnable()
        {
            RegisterCallbacks();
        }

        public void Update(float deltaTime)
        {
            if (targetObj == null)
            {
                return;
            }
            UpdateUIPosition(targetObj);
            UpdateLabel();
        }

        public void Start()
        {
        }


        public void Show(bool state)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;

        }

        public bool IsTarget(GameObject obj)
        {
            return obj != null && obj == targetObj;
        }

        public void SetTarget(GameObject target)
        {
            targetObj = target;
            if (targetObj.TryGetComponent<PlateauSandboxAdvertisementScaled>(out var adScaled))
            {
                adAssetSizeEditor.SetTarget(adScaled);

                // 2行目も表示
                column1.style.display = DisplayStyle.Flex;
                column2.style.display = DisplayStyle.Flex;

                // 1行目のUIを編集可能にする
                widthUpButton.style.display = DisplayStyle.Flex;
                widthDownButton.style.display = DisplayStyle.Flex;
                heightUpButton.style.display = DisplayStyle.Flex;
                heightDownButton.style.display = DisplayStyle.Flex;
                depthUpButton.style.display = DisplayStyle.Flex;
                depthDownButton.style.display = DisplayStyle.Flex;
                widthField.isReadOnly = false;
                heightField.isReadOnly = false;
                depthField.isReadOnly = false;

                // 2行目はcolumn2全体で有効無効を切り替えているので、
                // 2行目のUIは常時編集可
            }
            else
            {
                if (targetObj.TryGetComponent<PlateauSandboxAdvertisement>(out var ad))
                {
                    adAssetSizeEditor.SetTarget(ad);
                }
                else
                {
                    adAssetSizeEditor.ClearTarget();
                }

                // 1行目のみ表示
                column1.style.display = DisplayStyle.Flex;
                column2.style.display = DisplayStyle.None;

                // 1行目のUIを編集可能にする
                widthUpButton.style.display = DisplayStyle.Flex;
                widthDownButton.style.display = DisplayStyle.Flex;
                heightUpButton.style.display = DisplayStyle.Flex;
                heightDownButton.style.display = DisplayStyle.Flex;
                depthUpButton.style.display = DisplayStyle.Flex;
                depthDownButton.style.display = DisplayStyle.Flex;
                widthField.isReadOnly = false;
                heightField.isReadOnly = false;
                depthField.isReadOnly = false;
            }
        }

        private void RegisterCallbacks()
        {
            widthField.RegisterValueChangedCallback(OnSizeValueChanged);
            heightField.RegisterValueChangedCallback(OnSizeValueChanged);
            depthField.RegisterValueChangedCallback(OnSizeValueChanged);
            adWidthField.RegisterValueChangedCallback(OnAdWidthFieldChanged);
            adHeightField.RegisterValueChangedCallback(OnAdHeightFieldChanged);
            widthUpButton.clicked += OnWidthUpButtonClicked;
            widthDownButton.clicked += OnWidthDownButtonClicked;
            heightUpButton.clicked += OnHeightUpButtonClicked;
            heightDownButton.clicked += OnHeightDownButtonClicked;
            depthUpButton.clicked += OnDepthUpButtonClicked;
            depthDownButton.clicked += OnDepthDownButtonClicked;
            adWidthUpButton.clicked += OnAdWidthUpButtonClicked;
            adWidthDownButton.clicked += OnAdWidthDownButtonClicked;
            adHeightUpButton.clicked += OnAdHeightUpButtonClicked;
            adHeightDownButton.clicked += OnAdHeightDownButtonClicked;
        }

        private void UnregisterCallbacks()
        {
            widthField.UnregisterValueChangedCallback(OnSizeValueChanged);
            heightField.UnregisterValueChangedCallback(OnSizeValueChanged);
            depthField.UnregisterValueChangedCallback(OnSizeValueChanged);
            adWidthField.UnregisterValueChangedCallback(OnAdWidthFieldChanged);
            adHeightField.UnregisterValueChangedCallback(OnAdHeightFieldChanged);
            widthUpButton.clicked -= OnWidthUpButtonClicked;
            widthDownButton.clicked -= OnWidthDownButtonClicked;
            heightUpButton.clicked -= OnHeightUpButtonClicked;
            heightDownButton.clicked -= OnHeightDownButtonClicked;
            depthUpButton.clicked -= OnDepthUpButtonClicked;
            depthDownButton.clicked -= OnDepthDownButtonClicked;
            adWidthUpButton.clicked -= OnAdWidthUpButtonClicked;
            adWidthDownButton.clicked -= OnAdWidthDownButtonClicked;
            adHeightUpButton.clicked -= OnAdHeightUpButtonClicked;
            adHeightDownButton.clicked -= OnAdHeightDownButtonClicked;
        }

        private Vector3 GetGameObjectSize(GameObject gameObject)
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

        private bool SetGameObjectSize(GameObject root, Vector3 targetSize)
        {
            Debug.Log($"SetGameObjectSize: root = {root}, root.scale = {root.transform.localScale}, targetSize = {targetSize}");

            if (root == null)
            {
                Debug.LogWarning("SetGameObjectSize: root is null.");
                return false;
            }

            const float epsilon = 1e-4f;

            // Reject non-positive target sizes (adjust policy if zero sizes should be allowed)
            if (targetSize.x < epsilon || targetSize.y < epsilon || targetSize.z < epsilon)
            {
                Debug.LogWarning("SetGameObjectSize: targetSize contains too small component(s).");
                return false;
            }

            var currentSize = GetGameObjectSize(root);

            // Compute per-axis scale factors; keep axis unchanged if current size is ~0.
            var scaleFactor = new Vector3(
                currentSize.x > epsilon ? targetSize.x / currentSize.x : 1f,
                currentSize.y > epsilon ? targetSize.y / currentSize.y : 1f,
                currentSize.z > epsilon ? targetSize.z / currentSize.z : 1f
            );

            var t = root.transform;
            var newLocalScale = new Vector3(
                t.localScale.x * scaleFactor.x,
                t.localScale.y * scaleFactor.y,
                t.localScale.z * scaleFactor.z
            );

            // Validate result to avoid propagating invalid scale.
            if (float.IsNaN(newLocalScale.x) || float.IsInfinity(newLocalScale.x) ||
                float.IsNaN(newLocalScale.y) || float.IsInfinity(newLocalScale.y) ||
                float.IsNaN(newLocalScale.z) || float.IsInfinity(newLocalScale.z))
            {
                Debug.LogWarning("SetGameObjectSize: computed scale is invalid.");
                return false;
            }

            t.localScale = newLocalScale;
            return true;
        }

        private Vector3 GetBlankBuildingSize(GameObject root)
        {
            if (root.TryGetComponent<PlateauSandboxBuilding>(out var _))
            {
                var collider = root.GetComponentInChildren<BoxCollider>(true);
                if (collider == null)
                {
                    return GetGameObjectSize(root);
                }
                var size = collider.size.Scaled(collider.transform.lossyScale);
                return size;
            }
            else
            {
                return GetGameObjectSize(root);
            }
        }

        private bool SetBlankBuildingSize(GameObject root, Vector3 targetSize)
        {
            const float epsilon = 1e-4f;
            if (root == null) return false;
            if (targetSize.x < epsilon || targetSize.y < epsilon || targetSize.z < epsilon) return false;

            if (root.TryGetComponent<PlateauSandboxBuilding>(out var _))
            {
                var collider = root.GetComponentInChildren<BoxCollider>(true);
                if (collider == null)
                {
                    return SetGameObjectSize(root, targetSize);
                }

                var currentWorldSize = Vector3.Scale(collider.size, collider.transform.lossyScale);

                var scaleFactor = new Vector3(
                    currentWorldSize.x > epsilon ? targetSize.x / currentWorldSize.x : 1f,
                    currentWorldSize.y > epsilon ? targetSize.y / currentWorldSize.y : 1f,
                    currentWorldSize.z > epsilon ? targetSize.z / currentWorldSize.z : 1f
                );

                var t = root.transform;
                var newLocalScale = new Vector3(
                    t.localScale.x * scaleFactor.x,
                    t.localScale.y * scaleFactor.y,
                    t.localScale.z * scaleFactor.z
                );

                if (float.IsNaN(newLocalScale.x) || float.IsInfinity(newLocalScale.x) ||
                    float.IsNaN(newLocalScale.y) || float.IsInfinity(newLocalScale.y) ||
                    float.IsNaN(newLocalScale.z) || float.IsInfinity(newLocalScale.z))
                {
                    Debug.LogWarning("SetBlankBuildingSize: invalid computed scale.");
                    return false;
                }

                t.localScale = newLocalScale;
                return true;
            }

            return SetGameObjectSize(root, targetSize);
        }

        private void UpdateUIPosition(GameObject obj)
        {
            // オブジェクトの Bounds を取得
            if (!obj.TryGetComponent<Renderer>(out var renderer))
            {
                renderer = obj.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"Renderer が見つかりません: {obj.name}");
                    return;
                }
            }

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

            // 子要素の幅を取得（最初に見つかった子要素の幅を見た目の幅とする）
            float visualWidth = 0f;
            if (element.childCount > 0)
            {
                var firstChild = element[0];
                visualWidth = firstChild.resolvedStyle.width;
            }

            // UI 要素の上端中央を追従位置にするため、見た目の幅の半分をオフセット
            float offsetX = screenPos.x - visualWidth / 2;

            // Y座標のオフセット（下方向に少し移動）
            const float yOffset = 50f;
            float offsetY = Screen.height - screenPos.y + yOffset;

            // UI の位置を設定
            element.style.translate = new Translate(offsetX, offsetY);
        }
        private void UpdateLabel()
        {
            if (targetObj == null)
            {
                widthField.SetValueWithoutNotify(double.NaN);
                heightField.SetValueWithoutNotify(double.NaN);
                depthField.SetValueWithoutNotify(double.NaN);
            }
            else
            {
                Vector3 size = adAssetSizeEditor.CurrentTargetKind switch
                {
                    AdAssetSizeEditor.AdAssetKind.None => GetBlankBuildingSize(targetObj),
                    AdAssetSizeEditor.AdAssetKind.Single => adAssetSizeEditor.GetAdSize() ?? Vector3.zero,
                    AdAssetSizeEditor.AdAssetKind.Scaled => adAssetSizeEditor.GetAdSize() ?? Vector3.zero,
                    _ => Vector3.zero,
                };

                widthField.SetValueWithoutNotify(size.x);
                heightField.SetValueWithoutNotify(size.y);
                depthField.SetValueWithoutNotify(size.z);

                float? adWidth = adAssetSizeEditor.GetBillboardSize();
                adWidthField.SetValueWithoutNotify(adWidth ?? double.NaN);

                float? adHeight = adAssetSizeEditor.GetPoleHeight();
                adHeightField.SetValueWithoutNotify(adHeight ?? double.NaN);
            }
        }

        private void OnSizeValueChanged(ChangeEvent<double> evt)
        {
            var x = Mathf.Clamp((float)widthField.value, MinValue, MaxValue);
            widthField.SetValueWithoutNotify(x);
            var y = Mathf.Clamp((float)heightField.value, MinValue, MaxValue);
            heightField.SetValueWithoutNotify(y);
            var z = Mathf.Clamp((float)depthField.value, MinValue, MaxValue);
            depthField.SetValueWithoutNotify(z);

            if (adAssetSizeEditor.CurrentTargetKind != AdAssetSizeEditor.AdAssetKind.None)
            {
                adAssetSizeEditor.SetAdSize(new Vector3(x, y, z));
            }
            else
            {
                SetBlankBuildingSize(targetObj, new Vector3(x, y, z));
            }
        }

        private void OnAdWidthFieldChanged(ChangeEvent<double> evt)
        {
            var w = Mathf.Clamp((float)adWidthField.value, MinValue, MaxValue);
            adWidthField.SetValueWithoutNotify(w);
            adAssetSizeEditor.SetBillboardSize(w);
        }

        private void OnAdHeightFieldChanged(ChangeEvent<double> evt)
        {
            var h = Mathf.Clamp((float)adHeightField.value, MinValue, MaxValue);
            adHeightField.SetValueWithoutNotify(h);
            adAssetSizeEditor.SetPoleHeight(h);
        }

        private void OnWidthUpButtonClicked() => widthField.value += ButtonStep;
        private void OnWidthDownButtonClicked() => widthField.value -= ButtonStep;
        private void OnHeightUpButtonClicked() => heightField.value += ButtonStep;
        private void OnHeightDownButtonClicked() => heightField.value -= ButtonStep;
        private void OnDepthUpButtonClicked() => depthField.value += ButtonStep;
        private void OnDepthDownButtonClicked() => depthField.value -= ButtonStep;
        private void OnAdWidthUpButtonClicked() => adWidthField.value += ButtonStep;
        private void OnAdWidthDownButtonClicked() => adWidthField.value -= ButtonStep;
        private void OnAdHeightUpButtonClicked() => adHeightField.value += ButtonStep;
        private void OnAdHeightDownButtonClicked() => adHeightField.value -= ButtonStep;
    }
}
