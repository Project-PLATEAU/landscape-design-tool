using Landscape2.Runtime.UiCommon;
using System;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ProjectRegistModalUI
    {
        private VisualElement modalElement;
        private TextField projectNameFiled;
        private TextElement title;
        private Button registButton;
        private Button cancelButton;

        public Action<string> onRegist;

        public ProjectRegistModalUI(VisualElement element)
        {
            modalElement = new UIDocumentFactory().CreateWithUxmlName("Project_RegistModal");

            projectNameFiled = modalElement.Q<TextField>("ProjectName");
            title = modalElement.Q<TextElement>("ModalTitle");
            registButton = modalElement.Q<Button>("OKButton");
            cancelButton = modalElement.Q<Button>("CancelButton");

            RegisterEvents();
            Show(false);
        }

        private void RegisterEvents()
        {
            registButton.clicked += Regist;
            cancelButton.clicked += Cancel;
        }

        public void Show(bool isShow, string projectName = "", Action<string> callback = null)
        {
            // UIを表示する
            modalElement.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;

            if (isShow)
            {
                SetText(projectName);
                onRegist = callback;
            }
        }

        private void SetText(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                title.text = "プロジェクト新規作成";
                projectNameFiled.SetValueWithoutNotify("");
            }
            else
            {
                title.text = "プロジェクト名編集";
                projectNameFiled.SetValueWithoutNotify(projectName);
            }
        }

        private void Regist()
        {
            onRegist?.Invoke(projectNameFiled.text);
            Show(false);
        }
        
        private void Cancel()
        {
            Show(false);
        }
    }
}