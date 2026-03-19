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
        private Label widthLabel;
        private Label heightLabel;
        private Label depthLabel;
        private VisualElement row2;
        private Label adplaneLabel;
        private Label adpoleLabel;

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

            widthLabel = element.Q<Label>("width");
            heightLabel = element.Q<Label>("height");
            depthLabel = element.Q<Label>("depth");
            row2 = element.Q<VisualElement>("Row2");
            adplaneLabel = element.Q<Label>("adplane");
            adpoleLabel = element.Q<Label>("adpole");

            // カテゴリー名 + インデックスで名前を表現
            assetName.text = type.GetCategoryName() + "_" + typeIndex;

            Model = new ArrangementAssetListItem(prefabID, element, type);

            SetEditable(ProjectSaveDataManager.ProjectSetting.IsEditMode);

            RegisterButtons();

            row2.style.display = Model.IsAdScaled ? DisplayStyle.Flex : DisplayStyle.None;
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

        // public void SetAssetSize(Vector3 size)
        // {
        //     widthLabel.text = size.x.ToString("F2");
        //     heightLabel.text = size.y.ToString("F2");
        //     depthLabel.text = size.z.ToString("F2");
        // }

        // public void SetAdSize(float adplane, float adpole)
        // {
        //     adplaneLabel.text = adplane.ToString("F2");
        //     adpoleLabel.text = adpole.ToString("F2");
        // }

        public void UpdateLabels()
        {
            var size = Model.Size;
            widthLabel.text = size.x.ToString("F2");
            heightLabel.text = size.y.ToString("F2");
            depthLabel.text = size.z.ToString("F2");

            var (adplane, adpole) = Model.AdSize;
            adplaneLabel.text = adplane.ToString("F2");
            adpoleLabel.text = adpole.ToString("F2");
        }

    }
}