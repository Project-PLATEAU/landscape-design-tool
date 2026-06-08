using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.UiCommon
{
    /// <summary>
    /// 実行時にUIDocumentを生成します。
    /// </summary>
    public class UIDocumentFactory
    {
        private const string PanelSettingsName = "LandscapePanelSettings";
        private static readonly PanelSettings panelSettingsDefault = Resources.Load<PanelSettings>(PanelSettingsName);
        
        /// <summary>
        /// 新しいゲームオブジェクトを作り、そこにUIDocumentを付与し、
        /// Resourcesフォルダから<paramref name="uxmlName"/>を名前とするUXMLを読み込んで表示します。
        /// 生成したrootVisualElementを返します。
        /// </summary>
        public VisualElement CreateWithUxmlName(string uxmlName)
        {
            return CreateWithUxmlName(uxmlName, out var _);
        }

        /// <summary>
        /// 新しいゲームオブジェクトを作り、そこにUIDocumentを付与し、
        /// Resourcesフォルダから<paramref name="uxmlName"/>を名前とするUXMLを読み込んで表示します。
        /// 生成したrootVisualElementを返します。
        /// </summary>
        public VisualElement CreateWithUxmlName(string uxmlName, out GameObject obj)
        {
            var uiDocObj = new GameObject(uxmlName);
            obj = uiDocObj;
            var uiDocComponent = uiDocObj.AddComponent<UIDocument>();
            var panelSettings = panelSettingsDefault;
            if (panelSettings == null)
            {
                Debug.LogError("Panel Settings file is not found.");
                return null;
            }
            uiDocComponent.panelSettings = panelSettings;
            var uiRoot = uiDocComponent.rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>(uxmlName);
            if (visualTree == null)
            {
                Debug.LogError("Failed to load UXML file.");
                return null;
            }
            visualTree.CloneTree(uiRoot);
            return uiRoot;
        }

    }
}