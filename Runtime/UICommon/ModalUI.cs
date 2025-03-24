using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.UiCommon
{
    public static class ModalUI
    {
        private static VisualElement modalElement;
        private static VisualElement selectModalElement;

        private static Action onCloseModal = null;
        
        /// <summary>
        /// モーダルを表示。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="context"></param>
        /// <param name="isSuccess"></param>
        /// <param name="isFailed"></param>
        /// <param name="onClosed"></param>
        public static void ShowModal(
            string title,
            string context,
            bool isSuccess,
            bool isFailed,
            Action onClosed = null)
        {
            // アクションを上書き
            onCloseModal = onClosed;
            
            if (modalElement == null)
            {
                modalElement = new UIDocumentFactory().CreateWithUxmlName("Modal");
                GameObject.Find("Modal").GetComponent<UIDocument>().sortingOrder = 100;
                
                // イベント登録
                modalElement.Q<Button>("OKButton").clicked += () =>
                {
                    onCloseModal?.Invoke();
                    modalElement?.RemoveFromHierarchy();
                    modalElement = null;
                    GameObject.Destroy(GameObject.Find("Modal"));
                };
            }

            modalElement.Q<VisualElement>("Icon_Success").style.display = isSuccess ? DisplayStyle.Flex : DisplayStyle.None;
            modalElement.Q<VisualElement>("Icon_Error").style.display = isFailed ? DisplayStyle.Flex : DisplayStyle.None;

            modalElement.Q<Label>("ModalTitle").text = title;
            modalElement.Q<Label>("ModalText").text = context;
        }
        
        public enum SelectModalType
        {
            Info,
            Error,
            Success,
            Trash,
            TrashReverse,
        }
        
        private static Action onOkSelectModal = null;
        private static Action onCancelSelectModal = null;

        /// <summary>
        /// 選択モーダルを表示。
        /// </summary>
        /// <param name="title"></param>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <param name="onCancel"></param>
        /// <param name="onOk"></param>
        public static void ShowSelectModal(
            string title,
            string context,
            SelectModalType type,
            Action onCancel = null,
            Action onOk = null)
        {
            // アクション上書き
            onOkSelectModal = onOk;
            onCancelSelectModal = onCancel;
            
            if (selectModalElement == null)
            {
                selectModalElement = new UIDocumentFactory().CreateWithUxmlName("SelectModal");
                GameObject.Find("SelectModal").GetComponent<UIDocument>().sortingOrder = 100;
                
                // イベント登録
                selectModalElement.Q<Button>("CancelButton").clicked += () =>
                {
                    selectModalElement.style.display = DisplayStyle.None;
                    onCancelSelectModal?.Invoke();
                };
                
                selectModalElement.Q<Button>("OKButton").clicked += () =>
                {
                    selectModalElement.style.display = DisplayStyle.None;
                    onOkSelectModal?.Invoke();
                };
            }
            
            var iconSuccess = selectModalElement.Q<VisualElement>("Icon_Success");
            iconSuccess.style.display = type == SelectModalType.Success ? DisplayStyle.Flex : DisplayStyle.None;
            
            var iconError = selectModalElement.Q<VisualElement>("Icon_Error");
            iconError.style.display = type == SelectModalType.Error ? DisplayStyle.Flex : DisplayStyle.None;

            if (type == SelectModalType.Info)
            {
                // infoアイコンの色指定
                iconError.style.display = DisplayStyle.Flex;
                iconError.style.unityBackgroundImageTintColor = new StyleColor(new Color(156 / 255f, 241 / 255f, 240 / 255f, 1.0f));
            }

            var iconTrash = selectModalElement.Q<VisualElement>("Icon_Trash");
            iconTrash.style.display = type == SelectModalType.Trash ? DisplayStyle.Flex : DisplayStyle.None;
            
            var iconTrashReverse = selectModalElement.Q<VisualElement>("Icon_TrashReverse");
            iconTrashReverse.style.display = type == SelectModalType.TrashReverse ? DisplayStyle.Flex : DisplayStyle.None;

            selectModalElement.Q<Label>("ModalTitle").text = title;
            selectModalElement.Q<Label>("ModalText").text = context;
            
            // 表示
            selectModalElement.style.display = DisplayStyle.Flex;
        }
    }
}