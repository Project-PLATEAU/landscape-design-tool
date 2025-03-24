using PlateauToolkit.Sandbox.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// フィールド選択のUI
    /// </summary>
    public class BulkArrangementAssetFieldUI
    {
        private struct DropDownInfo
        {
            public DropdownField dropdown;
            public VisualElement containerElement;
            public string selectedValue;
            
            public void RegisterValueChangedCallback(EventCallback<ChangeEvent<string>> callback)
            {
                dropdown.RegisterValueChangedCallback(callback);
            }
            
            public void Show(bool isShow)
            {
                containerElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            public void SetChoices(string[] choices)
            {
                dropdown.choices.Clear();
                foreach (var choice in choices)
                {
                    dropdown.choices.Add(choice);
                }
            }
            
            public void SetValue(int index)
            {
                dropdown.value = dropdown.choices[index];
                selectedValue = dropdown.choices[index];
            }
        }
        
        private BulkArrangementAsset bulkArrangementAsset;
        private VisualElement UIElement;
        
        // プルダウン情報
        private DropDownInfo heightDropDownInfo = new();
        private DropDownInfo latitudeDropDownInfo = new();
        private DropDownInfo longitudeDropDownInfo = new();
        private DropDownInfo heightFieldDropDownInfo = new();
        private DropDownInfo assetTypeDropDownInfo = new();
        
        public BulkArrangementAssetFieldUI(VisualElement element, BulkArrangementAsset bulkArrangementAssetInstance)
        {
            bulkArrangementAsset = bulkArrangementAssetInstance;
            var assetImportElement = element.Q<VisualElement>("Panel_AssetImport");
            UIElement = assetImportElement.Q<VisualElement>("SettingContainer");
            
            RegisterButtons();
            Show(false);
        }

        private void RegisterButtons()
        {
            // 高さプルダウン
            heightDropDownInfo.dropdown = UIElement.Q<DropdownField>("IgnoreList");
            heightDropDownInfo.containerElement = UIElement.Q<VisualElement>("IgnoreContainer");
            heightDropDownInfo.RegisterValueChangedCallback((evt ) => OnChangeHeight(evt.newValue));
            heightDropDownInfo.Show(false);
            
            // 緯度プルダウン
            latitudeDropDownInfo.dropdown = UIElement.Q<DropdownField>("latitudeList");
            latitudeDropDownInfo.containerElement = UIElement.Q<VisualElement>("LatitudeContainer");
            latitudeDropDownInfo.RegisterValueChangedCallback((evt ) => 
                OnChangeLatitude(latitudeDropDownInfo.selectedValue, evt.newValue));
            latitudeDropDownInfo.Show(false);
            
            // 経度プルダウン
            longitudeDropDownInfo.dropdown = UIElement.Q<DropdownField>("longitudeList");
            longitudeDropDownInfo.containerElement = UIElement.Q<VisualElement>("LongitudeContainer");
            longitudeDropDownInfo.RegisterValueChangedCallback((evt ) => 
                OnChangeLongitude(longitudeDropDownInfo.selectedValue, evt.newValue));
            longitudeDropDownInfo.Show(false);
            
            // 高さ設定プルダウン
            heightFieldDropDownInfo.dropdown = UIElement.Q<DropdownField>("HeightList");
            heightFieldDropDownInfo.containerElement = UIElement.Q<VisualElement>("HeightContainer");
            heightFieldDropDownInfo.RegisterValueChangedCallback((evt ) => 
                OnChangeHeightField(heightFieldDropDownInfo.selectedValue, evt.newValue));
            heightFieldDropDownInfo.Show(false);
            
            // アセット種別プルダウン
            assetTypeDropDownInfo.dropdown = UIElement.Q<DropdownField>("AssetTypeList");
            assetTypeDropDownInfo.containerElement = UIElement.Q<VisualElement>("AssetTypeContainer");
            assetTypeDropDownInfo.RegisterValueChangedCallback((evt ) => 
                OnChangeAssetType(assetTypeDropDownInfo.selectedValue, evt.newValue));
            assetTypeDropDownInfo.Show(false);
        }

        public void Show(bool isShow)
        {
            // UIを表示する
            UIElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isShow)
            {
                return;
            }

            SetHeightDropDown();
            SetDropDown();
        }
        
        private void SetHeightDropDown()
        {
            heightDropDownInfo.SetChoices(new string[] {"ファイルで指定した高さに設定", "地面に配置"});
            heightDropDownInfo.SetValue(0);
        }

        private void SetDropDown()
        {
            // アセット種別は共通で表示
            assetTypeDropDownInfo.Show(true);
            
            // CSVファイルの場合は高さ、緯度、経度、高さフィールドを表示
            heightDropDownInfo.Show(bulkArrangementAsset.IsLoadedCsv());
            latitudeDropDownInfo.Show(bulkArrangementAsset.IsLoadedCsv());
            longitudeDropDownInfo.Show(bulkArrangementAsset.IsLoadedCsv());
            heightFieldDropDownInfo.Show(bulkArrangementAsset.IsLoadedCsv());
            
            // それぞれのプルダウン更新
            var choices = bulkArrangementAsset.GetFieldLabels();
            
            assetTypeDropDownInfo.SetChoices(choices);
            assetTypeDropDownInfo.SetValue(bulkArrangementAsset.GetFieldIndex(PlateauSandboxBulkPlaceCategory.k_AssetType));
            latitudeDropDownInfo.SetChoices(choices);
            latitudeDropDownInfo.SetValue(bulkArrangementAsset.GetFieldIndex(PlateauSandboxBulkPlaceCategory.k_Latitude));
            longitudeDropDownInfo.SetChoices(choices);
            longitudeDropDownInfo.SetValue(bulkArrangementAsset.GetFieldIndex(PlateauSandboxBulkPlaceCategory.k_Longitude));
            heightFieldDropDownInfo.SetChoices(choices);
            heightFieldDropDownInfo.SetValue(bulkArrangementAsset.GetFieldIndex(PlateauSandboxBulkPlaceCategory.k_Height));
            
            // データをロード
            bulkArrangementAsset.LoadAssetTypes();
        }
        
        private void ReplacePullDownChoices(DropdownField dropdown, string oldValue, string newValue, int categoryIndex)
        {
            if (oldValue == newValue)
            {
                return;
            }
            var oldIndex = dropdown.choices.IndexOf(oldValue);
            var newIndex = dropdown.choices.IndexOf(newValue);
            
            bulkArrangementAsset.ReplaceField(oldIndex, newIndex);
            SetDropDown();
        }

        private void OnChangeHeight(string newValue)
        {
            var newIndex = heightDropDownInfo.dropdown.choices.IndexOf(newValue);
            bulkArrangementAsset.SetIgnoreHeight(newIndex == 1);
        }
        
        private void OnChangeLatitude(string oldValue, string newValue)
        {
            ReplacePullDownChoices(latitudeDropDownInfo.dropdown, oldValue, newValue, (int)PlateauSandboxBulkPlaceCategory.k_Latitude);
        }
        
        private void OnChangeLongitude(string oldValue, string newValue)
        {
            ReplacePullDownChoices(longitudeDropDownInfo.dropdown, oldValue, newValue, (int)PlateauSandboxBulkPlaceCategory.k_Longitude);
        }
        
        private void OnChangeHeightField(string oldValue, string newValue)
        {
            ReplacePullDownChoices(heightFieldDropDownInfo.dropdown, oldValue, newValue, (int)PlateauSandboxBulkPlaceCategory.k_Height);
        }
        
        private void OnChangeAssetType(string oldValue, string newValue)
        {
            ReplacePullDownChoices(assetTypeDropDownInfo.dropdown, oldValue, newValue, (int)PlateauSandboxBulkPlaceCategory.k_AssetType);
        }
    }
}