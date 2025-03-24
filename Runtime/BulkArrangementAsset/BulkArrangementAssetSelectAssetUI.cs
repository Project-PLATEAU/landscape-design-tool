using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BulkArrangementAssetSelectAssetUI
    {
        private BulkArrangementAsset bulkArrangementAsset;
        private VisualElement UIElement;
        private ScrollView assetListScrollView;
        private VisualTreeAsset thumbnailElement;
        private DropdownField categoryDropdown;

        public BulkArrangementAssetSelectAssetUI(VisualElement element, BulkArrangementAsset bulkArrangementAssetInstance)
        {
            bulkArrangementAsset = bulkArrangementAssetInstance;
            UIElement = element.Q<VisualElement>("Panel_AssetSelect");
            assetListScrollView = UIElement.Q<ScrollView>("Panel_Asset");
            thumbnailElement = Resources.Load<VisualTreeAsset>("Thumbnail_Asset");

            RegisterButtons();
            Show(false);
        }

        private void RegisterButtons()
        {
            // カテゴリープルダウン
            categoryDropdown = UIElement.Q<DropdownField>("Panel_Category");
            categoryDropdown.choices.Add(ArrangementAssetType.Plant.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.Advertisement.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.Human.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.Vehicle.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.Building.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.StreetFurniture.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.Sign.GetCategoryName());
            categoryDropdown.choices.Add(ArrangementAssetType.Miscellaneous.GetCategoryName());

            categoryDropdown.RegisterValueChangedCallback((evt) => 
                OnChangeCategory(categoryDropdown.choices.IndexOf(evt.newValue)));
        }

        public void TryShow(bool isShow)
        {
            if (UIElement.style.display == DisplayStyle.None && isShow)
            {
                Show(true);
            }
            else if (UIElement.style.display == DisplayStyle.Flex && !isShow)
            {
                Show(false);
            }
        }

        public void Show(bool isShow)
        {
            UIElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;

            if (isShow)
            {
                // カテゴリーを樹木で表示
                categoryDropdown.index = 0;
                DrawAssets(ArrangementAssetType.Plant);
            }
        }

        private void OnChangeCategory(int selectIndex)
        {
            assetListScrollView.Clear();
            DrawAssets((ArrangementAssetType)selectIndex);
        }

        private void DrawAssets(ArrangementAssetType selectType)
        {
            var assetList = ArrangementAssetLoader.AssetDictionary[selectType];
            
            foreach (var asset in assetList)
            {
                var newElement = thumbnailElement.CloneTree();
                var button = newElement.Q<Button>("Thumbnail_Asset");
                button.style.backgroundImage = ArrangementAssetLoader.GetPicture(asset.name);
                button.clicked += () =>
                {
                    // IDを設定
                    bulkArrangementAsset.SetPrefabId(asset.GetInstanceID());
                };
                
                assetListScrollView.Add(newElement);
            }
        }
    }
}