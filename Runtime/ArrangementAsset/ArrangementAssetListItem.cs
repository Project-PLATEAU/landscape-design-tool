using PlateauToolkit.Sandbox.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 配置したアセットの一覧のモデル
    /// </summary>
    public class ArrangementAssetListItem
    {
        public int PrefabID { get; private set; }
        public VisualElement Element { get; private set; }
        public ArrangementAssetType Type { get; private set; }
        public GameObject Target { get; private set; }
        public bool IsAdScaled => adSizeEditor.CurrentTargetKind == AdAssetSizeEditor.AdAssetKind.Scaled;
        public Vector3 Size => GetSize();
        public (float adplane, float adpole) AdSize => GetAdSize();

        private readonly AdAssetSizeEditor adSizeEditor = new();

        public ArrangementAssetListItem(
            int prefabID,
            VisualElement element,
            ArrangementAssetType type)
        {
            PrefabID = prefabID;
            Element = element;
            Type = type;
            Target = GetAsset(prefabID);

            if (Type == ArrangementAssetType.Advertisement)
            {
                if (Target.TryGetComponent<PlateauSandboxAdvertisementScaled>(out var adScaled))
                {
                    adSizeEditor.SetTarget(adScaled);
                }
                else if (Target.TryGetComponent<PlateauSandboxAdvertisement>(out var ad))
                {
                    adSizeEditor.SetTarget(ad);
                }
            }
        }

        private GameObject GetAsset(int prefabID)
        {
            var createdAssets = GameObject.Find("CreatedAssets");
            if (createdAssets == null)
            {
                Debug.LogWarning("CreatedAssetsが見つかりませんでした");
                return null;
            }

            // CreatedAssetsの子オブジェクトから取得
            foreach (Transform child in createdAssets.transform)
            {
                if (child.gameObject.GetInstanceID() == prefabID)
                {
                    return child.gameObject;
                }
            }

            return null;
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

        private Vector3 GetSize()
        {
            if (adSizeEditor.CurrentTargetKind != AdAssetSizeEditor.AdAssetKind.None)
            {
                return adSizeEditor.GetAdSize() ?? Vector3.zero;
            }
            else
            {
                return GetGameObjectSize(Target);
            }
        }

        private (float adplane, float adpole) GetAdSize()
        {
            if (adSizeEditor.CurrentTargetKind == AdAssetSizeEditor.AdAssetKind.Scaled)
            {
                var adplane = adSizeEditor.GetBillboardSize() ?? 0f;
                var adpose = adSizeEditor.GetPoleHeight() ?? 0f;
                return (adplane, adpose);
            }
            else
            {
                return (0f, 0f);
            }
        }
    }
}