using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 配置したアセットの一覧のプレゼンター
    /// </summary>
    public class ArrangementAssetListUI
    {
        private VisualTreeAsset itemVisualTree;
        private List<ArrangementAssetListItemUI> itemUIs = new();

        private VisualElement noAssets;
        private VisualElement listContent;

        private ArrangementAssetFocus focus;

        public UnityEvent<GameObject> OnDeleteAsset = new();

        // 外から通知を受け取るようにstaticに
        public static UnityEvent<GameObject> OnCreatedAsset = new();
        public static UnityEvent<GameObject> OnCancelAsset = new();

        public ArrangementAssetListUI(VisualElement element, LandscapeCamera landscapeCamera)
        {
            var assetLeftList = element.Q<VisualElement>("Panel_AssetLeftList");
            listContent = assetLeftList.Q<VisualElement>("Panel");
            noAssets = assetLeftList.Q<VisualElement>("Dialogue");
            noAssets.style.display = DisplayStyle.Flex;
            itemVisualTree = Resources.Load<VisualTreeAsset>("List_Asset");
            focus = new ArrangementAssetFocus(landscapeCamera);

            OnCreatedAsset.AddListener(AddAsset);
            OnCancelAsset.AddListener((go) => RemoveAsset(go.GetInstanceID()));
        }

        private void AddAsset(GameObject target)
        {
            if (itemUIs.Any(item => item.Model.PrefabID == target.GetInstanceID()))
            {
                Debug.LogWarning("すでに追加されているアセットです");
                return;
            }

            var placeObject = GetAsset(target.GetInstanceID());
            if (placeObject == null)
            {
                Debug.LogWarning("配置対象のオブジェクトが見つかりませんでした");
                return;
            }

            Debug.Log($"アセットを追加しました。{target.name}");

            // LODGroupが付いているAssetは全てdisableにする
            if (target.TryGetComponent<LODGroup>(out var lodGroup))
            {
                lodGroup.enabled = false;
            }

            var type = ArrangementAssetTypeExtensions.GetArrangementAssetType(target);
            var typeCount = itemUIs.Count(item => item.Model.Type == type);
            typeCount++;

            var item = itemVisualTree.CloneTree();
            var itemUI = new ArrangementAssetListItemUI(target.GetInstanceID(), item, type, typeCount);

            // 削除コールバック
            itemUI.OnDeleteAsset.AddListener(() =>
            {
                RemoveAsset(target.GetInstanceID());
            });

            // フォーカスコールバック
            itemUI.OnFocusAsset.AddListener(() =>
            {
                // フォーカス処理
                OnFocusAsset(target.GetInstanceID());
            });

            listContent.Add(itemUI.Model.Element);
            itemUIs.Add(itemUI);

            TryShowNoAssets();
        }

        public void RemoveAsset(int prefabID)
        {
            if (itemUIs.Count == 0)
            {
                return;
            }
            var ui = GetItemUI(prefabID);
            if (ui == null)
            {
                return;
            }
            listContent.Remove(ui.Model.Element);
            itemUIs.Remove(ui);
            RemoveFromScene(prefabID);

            TryShowNoAssets();

            // プロジェクトへ通知
            ProjectSaveDataManager.Delete(ProjectSaveDataType.Asset, prefabID.ToString());
        }

        private void RemoveFromScene(int prefabID)
        {
            var asset = GetAsset(prefabID);
            if (asset == null)
            {
                Debug.LogWarning("削除対象のアセットが見つかりませんでした");
                return;
            }
            OnDeleteAsset.Invoke(asset);
        }

        private void OnFocusAsset(int prefabID)
        {
            // フォーカス処理
            var asset = GetAsset(prefabID);
            if (asset == null)
            {
                Debug.LogWarning("削除対象のアセットが見つかりませんでした");
                return;
            }
            focus.Focus(asset.transform);
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

        private void TryShowNoAssets()
        {
            var isNoAssets = itemUIs.Count == 0 || itemUIs.All(item => !item.IsShow);
            noAssets.style.display = isNoAssets ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show(bool isShow, int prefabID)
        {
            if (itemUIs.Count == 0)
            {
                return;
            }

            var ui = GetItemUI(prefabID);
            ui?.Show(isShow);

            TryShowNoAssets();
        }

        public void SetEditable(bool isEditable, int prefabID)
        {
            if (itemUIs.Count == 0)
            {
                return;
            }

            var ui = GetItemUI(prefabID);
            ui?.SetEditable(isEditable);
        }

        private ArrangementAssetListItemUI GetItemUI(int prefabID)
        {
            var ui = itemUIs.Find(item => item.Model.PrefabID == prefabID);
            if (ui == null)
            {
                Debug.LogWarning("該当のアセットが見つかりませんでした");
                return null;
            }
            return ui;
        }
    }
}