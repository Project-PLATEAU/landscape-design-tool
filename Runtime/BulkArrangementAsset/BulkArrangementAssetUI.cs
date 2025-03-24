using PlateauToolkit.Sandbox.Runtime;
using SFB;
using System;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BulkArrangementAssetUI
    {
        private BulkArrangementAsset bulkArrangementAsset = new();
        private BulkArrangementAssetFieldUI fieldUI;
        private BulkArrangementAssetSelectUI selectUI;
        private BulkArrangementAssetPlaceUI placeUI;
        private VisualElement UIElement;
        
        public BulkArrangementAssetUI(VisualElement element)
        {
            UIElement = element.Q<VisualElement>("Panel_AssetImport");
            fieldUI = new BulkArrangementAssetFieldUI(element, bulkArrangementAsset);
            selectUI = new BulkArrangementAssetSelectUI(element, bulkArrangementAsset);
            placeUI = new BulkArrangementAssetPlaceUI(element, bulkArrangementAsset);
            
            // アセットロード
            ArrangementAssetLoader.LoadAssets();
            
            RegisterButtons();
            ShowFields(false);
            Show(false);
        }

        private void RegisterButtons()
        {
            // CSVファイル読み込みボタン
            var csvLoadButton = UIElement.Q<Button>("CSVImportButton");
            csvLoadButton.clicked += () =>
            {
                LoadFile(true);
            };
            
            // Shapeファイル読み込みボタン
            var shapeLoadButton = UIElement.Q<Button>("ShapeImportButton");
            shapeLoadButton.clicked += () =>
            {
                LoadFile(false);
            };
            
            // キャンセルボタン
            var cancelButton = UIElement.Q<Button>("CancelButton");
            cancelButton.clicked += () =>
            {
                ShowFields(false);
            };
        }

        public void Show(bool isShow)
        {
            // UIを表示する
            UIElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isShow)
            {
                // 非表示時はアセットリストを非表示
                selectUI.TryShowAssetList(false);
                
                // アセット選択状態をリセット
                selectUI.ResetSelect();
            }
        }

        private void ShowFields(bool isShow)
        {
            fieldUI.Show(isShow);
            selectUI.Show(isShow);
            placeUI.Show(isShow);
            
            if (!isShow)
            {
                // 非表示時はアセットリストを非表示
                selectUI.TryShowAssetList(false);
                bulkArrangementAsset.Reset();
            }
        }

        private void CreateCsvTemplate()
        {
            var path = StandaloneFileBrowser.SaveFilePanel("Create CSV Template","","","csv");
            PlateauSandboxCsvTemplate.Create(path);
        }
        
        private void LoadFile(bool isCsv)
        {
            string[] paths;
#if UNITY_EDITOR && UNITY_STANDALONE_OSX 
            // NOTE: macで開発時用にEditorのファイル選択ダイアログを表示
            var openFile = UnityEditor.EditorUtility.OpenFilePanel("Select File", "", isCsv ? "csv" : "shp");
            paths = new string[] { openFile };
#else
            paths = StandaloneFileBrowser.OpenFilePanel("Select File", "",isCsv ? "csv" : "shp", false);
#endif
            string path = "";
            if(paths.Length > 0)
            {
                path =  paths[0];
            }

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // パスからファイルを読み込む
            var isLoadSuccess = bulkArrangementAsset.TryLoadFile(path);
            if (isLoadSuccess)
            {
                // フィールドとアセット選択UIを表示
                ShowFields(true);
            }
        }
    }
}