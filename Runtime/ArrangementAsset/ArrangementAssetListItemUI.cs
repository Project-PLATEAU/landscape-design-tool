using Landscape2.Runtime.UiCommon;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 配置したアセットの一覧のアイテム
    /// </summary>
    public class ArrangementAssetListItemUI
    {
        public ArrangementAssetListItem Model { get; private set; }
        
        public UnityEvent OnDeleteAsset = new();
        public UnityEvent OnFocusAsset = new();
        
        private bool isShow = true;
        public bool IsShow => isShow;
        
        public ArrangementAssetListItemUI(
            int prefabID,
            VisualElement element,
            ArrangementAssetType type,
            int typeIndex)
        {
            var assetName = element.Q<Label>("AssetName");
            
            // カテゴリー名 + インデックスで名前を表現
            assetName.text = type.GetCategoryName() + "_" + typeIndex;
            
            Model = new ArrangementAssetListItem(prefabID, element, type);
            
            SetEditable(ProjectSaveDataManager.ProjectSetting.IsEditMode);
            
            RegisterButtons();
        }

        private void RegisterButtons()
        {
            var deleteButton = Model.Element.Q<Button>("DeleteButton");
            deleteButton.clicked += () =>
            {
                OnDeleteAsset.Invoke();
            };
            
            var focusButton = Model.Element.Q<Button>("List");
            focusButton.clicked += () =>
            {
                OnFocusAsset.Invoke();
            };
        }
        
        public void Show(bool isShow)
        {
            Model.Element.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
            this.isShow = isShow;
        }
        
        public void SetEditable(bool isEditable)
        {
            Model.Element.Q<Button>("DeleteButton").style.display = 
                isEditable ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}