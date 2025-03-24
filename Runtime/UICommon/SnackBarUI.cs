using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Landscape2.Runtime.UiCommon
{
    /// <summary>
    /// 共通用のSnackBarUI
    /// </summary>
    public class SnackBarUI
    {
        public static readonly string NotEditWarning = "編集できません。";
        
        private static VisualTreeAsset snackBarAsset;
        private static VisualElement root;
        
        public static void Show(string message, VisualElement parentTarget)
        {
            LoadAsset();
            root = parentTarget;
            if (root == null || snackBarAsset == null)
            {
                return;
            }

            if (root.Q<VisualElement>("Snackbar") != null)
            {
                Hide();
            }

            var snackBarClone = snackBarAsset.CloneTree();
            parentTarget.Add(snackBarClone);
            
            var closeButton = snackBarClone.Q<Button>("CloseButton");
            snackBarClone.Q<Label>("SnackbarText").text = message;
            closeButton.clicked += Hide;
            
            DelayHide();
        }
        
        private static void LoadAsset()
        {
            if (snackBarAsset == null)
            {
                snackBarAsset = Resources.Load<VisualTreeAsset>("Snackbar");
            }
        }

        private static async void DelayHide()
        {
            await Task.Delay(3000); // 3秒待つ
            Hide();
        }

        private static void Hide()
        {
            var snackBarClone = root.Q<VisualElement>("Snackbar");
            if (snackBarClone != null)
            {
                snackBarClone.RemoveFromHierarchy();
            }
        }
    }
}