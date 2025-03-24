using Landscape2.Runtime.UiCommon;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BulkArrangementAssetPlaceUI
    {
        private BulkArrangementAsset bulkArrangementAsset;
        private VisualElement UIElement;
        private BulkArrangementAssetPlace bulkArrangementAssetPlace = new();
        
        public BulkArrangementAssetPlaceUI(VisualElement element, BulkArrangementAsset bulkArrangementAssetInstance)
        {
            bulkArrangementAsset = bulkArrangementAssetInstance;
            var assetImportElement = element.Q<VisualElement>("Panel_AssetImport");
            UIElement = assetImportElement.Q<VisualElement>("ActionContainer");
            RegisterButtons();
            Show(false);
        }

        private void RegisterButtons()
        {
            // 配置実行ボタン
            var placeButton = UIElement.Q<Button>("OKButton");
            placeButton.clicked += async () =>
            {
                ShowProgress();
                var resultText = await bulkArrangementAssetPlace.PlaceAll(bulkArrangementAsset);
                ShowResultDialog(resultText);
                
                // リストに追加
                bulkArrangementAssetPlace.TrySetAssetList();
                
                // アセット配置終了処理
                bulkArrangementAssetPlace.Stop();
            };
        }

        public void Show(bool isShow)
        {
            // UIを表示する
            UIElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void ShowProgress()
        {
            ModalUI.ShowModal("アセット一括配置", "アセットの配置中...", false, false, () =>
            {
                // アセット配置キャンセル
                bulkArrangementAssetPlace.Stop();
            });
        }
        
        private void ShowResultDialog(string resultText)
        {
            var title = "アセット一括配置";
            if (!string.IsNullOrEmpty(resultText))
            {
                ModalUI.ShowModal("アセット一括配置", resultText, false, true);
            }
            else if (bulkArrangementAssetPlace.IsAllPlaceSuccess())
            {
                ModalUI.ShowModal(title, "全てのアセットの配置に成功しました", true, false);
            }
            else if (bulkArrangementAssetPlace.IsAllPlaceFailed())
            {
                ModalUI.ShowModal(title, "全てのアセットの配置に失敗しました。", false, true);
            }
            else
            {
                ModalUI.ShowModal(title, "一部のアセットの配置に失敗しました。", true, false);
            }
        }
    }
}