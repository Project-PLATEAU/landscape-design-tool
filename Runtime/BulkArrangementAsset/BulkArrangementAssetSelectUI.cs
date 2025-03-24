using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Image = UnityEngine.UIElements.Image;

namespace Landscape2.Runtime
{
    /// <summary>
    /// アセット選択UI
    /// </summary>
    public class BulkArrangementAssetSelectUI
    {
        private BulkArrangementAsset bulkArrangementAsset;
        private VisualElement UIElement;
        private ScrollView scrollView;
        private VisualTreeAsset itemElement;
        private BulkArrangementAssetSelectAssetUI assetSelectAssetUI;
        
        public BulkArrangementAssetSelectUI(VisualElement element, BulkArrangementAsset bulkArrangementAssetInstance)
        {
            bulkArrangementAsset = bulkArrangementAssetInstance;
            bulkArrangementAsset.OnUpdatedSelectAsset.AddListener(UpdateSelectImage);
            
            assetSelectAssetUI = new BulkArrangementAssetSelectAssetUI(element, bulkArrangementAssetInstance);
            var assetImportElement = element.Q<VisualElement>("Panel_AssetImport");
            UIElement = assetImportElement.Q<VisualElement>("AssetContainer");
            scrollView = assetImportElement.Q<ScrollView>("ImportAssetScrollView");
            itemElement = Resources.Load<VisualTreeAsset>("List_ImportAssetList");
            
            // 更新されたら一覧を再描画
            bulkArrangementAsset.OnUpdatedAssetTypes.AddListener(DrawScrollView);
            
            Show(false);
        }

        public void Show(bool isShow)
        {
            // UIを表示する
            UIElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void DrawScrollView()
        {
            ResetSelect();
            scrollView.Clear();
            
            foreach (var item in bulkArrangementAsset.AssetTypes)
            {
                var newElement = itemElement.CloneTree();
            
                // IDを名前で設定
                newElement.name = item.ID.ToString();
                
                // カテゴリー名とアイテム数を設定
                var categoryLabel = newElement.Q<Label>("Li_Name");
                categoryLabel.text = item.CategoryName;
                
                var countLabel = newElement.Q<Label>("Li_Number");
                countLabel.text = item.Count.ToString();
                
                // クリックイベントを設定
                Button selectButton = newElement.Q<Button>("List");
                selectButton.clicked += () =>
                {
                    if (bulkArrangementAsset.SelectedID == item.ID)
                    {
                        // すでに選択中
                        return;
                    }

                    // 選択中のボタンを非アクティブに
                    DeactivateAllButtons();

                    // 選択中のボタンをアクティブに
                    selectButton.AddToClassList("active");
                    
                    // IDを設定
                    bulkArrangementAsset.SetSelectedID(item.ID);
                    
                    // アセットリストが開いてなければ表示
                    TryShowAssetList(true);
                };
                
                // 画像を表示する要素
                var imageElement = newElement.Q<VisualElement>("Thumbnail_Asset");
                imageElement.style.display = DisplayStyle.None;
                
                // 未選択の場合
                var noImageElement = newElement.Q<VisualElement>("Li_NoImage");
                noImageElement.style.display = DisplayStyle.None;
                
                if (item.PrefabConstantID > 0)
                {
                    // 選択中のアイテムの画像を表示
                    DrawSelectImage(imageElement, item.PrefabConstantID);
                    ShowThumbnail(imageElement, noImageElement, true);
                }
                else
                {
                    // 未選択の場合は画像を非表示
                    ShowThumbnail(imageElement, noImageElement, false);
                }

                // スクロールビューにアイテムを追加
                scrollView.contentContainer.Add(newElement);
            }
        }

        private void DrawSelectImage(VisualElement imageElement, int selectPrefabID)
        {
            imageElement.style.backgroundImage = ArrangementAssetLoader.GetPicture(selectPrefabID);
        }
        
        private void UpdateSelectImage(int selectPrefabID)
        {
            foreach (var child in scrollView.contentContainer.Children())
            {
                if (child.name == bulkArrangementAsset.SelectedID.ToString())
                {
                    var imageElement = child.Q<VisualElement>("Thumbnail_Asset");
                    var noImageElement = child.Q<VisualElement>("Li_NoImage");
                    DrawSelectImage(imageElement, selectPrefabID);
                    
                    ShowThumbnail(imageElement, noImageElement, true);
                }
            }
        }
        
        private void ShowThumbnail(VisualElement thumbnail, VisualElement noImage, bool isShow)
        {
            // サムネイル画像を表示する
            thumbnail.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            noImage.style.display = isShow ? DisplayStyle.None : DisplayStyle.Flex;
        }
        
        public void TryShowAssetList(bool isShow)
        {
            assetSelectAssetUI.TryShow(isShow);
        }

        private void DeactivateAllButtons()
        {
            foreach (var child in scrollView.contentContainer.Children())
            {
                var selectButton = child.Q<Button>("List");
                selectButton.RemoveFromClassList("active");
            }
        }
        
        public void ResetSelect()
        {
            DeactivateAllButtons();
            bulkArrangementAsset.SetSelectedID(-1);
        }
    }
}