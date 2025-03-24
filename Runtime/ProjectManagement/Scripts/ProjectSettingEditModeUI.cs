using Landscape2.Runtime.UiCommon;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// プロジェクト編集モード / 閲覧モード切り替え
    /// </summary>
    public class ProjectSettingEditModeUI
    {
        private VisualElement element;
        private Toggle switchButton;
        
        public UnityEvent<bool> OnEditModeChanged { get; } = new ();
        
        public ProjectSettingEditModeUI(VisualElement element)
        {
            this.element = element;
            switchButton = element.Q<Toggle>("Toggle_EditMode");
            
            RegisterEvents();
        }
        
        private void RegisterEvents()
        {
            switchButton.RegisterValueChangedCallback(evt =>
            {
                var isEdit = evt.newValue;
                
                // 状態を切り替え
                ProjectSaveDataManager.ProjectSetting.SetEditMode(isEdit);
                
                // 通知
                OnEditModeChanged.Invoke(isEdit);
            });
        }
    }
}