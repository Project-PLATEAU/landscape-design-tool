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
            var uiDocObj = new GameObject(uxmlName);
            var uiDocComponent = uiDocObj.AddComponent<UIDocument>();
            var panelSettings = panelSettingsDefault;
            if (panelSettings == null)
            {
                Debug.LogError("Panel Settings file is not found.");
            }
            uiDocComponent.panelSettings = panelSettings;
            var uiRoot = uiDocComponent.rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>(uxmlName);
            if (visualTree == null)
            {
                Debug.LogError("Failed to load UXML file.");
            }
            visualTree.CloneTree(uiRoot);
            return uiRoot;
        }
    }
}