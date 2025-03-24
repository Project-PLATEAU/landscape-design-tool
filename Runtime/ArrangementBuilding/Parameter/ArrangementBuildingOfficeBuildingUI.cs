using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingOfficeBuildingUI : IArrangementBuildingParameterUI
    {
        public UnityEvent OnUpdated = new UnityEvent();
        private VisualElement editPanel;
        
        private ArrangementBuildingOfficeBuilding officeBuilding;
        private Slider panelHeightSlider;
        private Toggle isChangeWindow;
        
        public ArrangementBuildingOfficeBuildingUI(VisualElement element)
        {
            officeBuilding = new ArrangementBuildingOfficeBuilding();
            RegisterButtons(element);
        }

        private void RegisterButtons(VisualElement element)
        {
            editPanel = element.Q<VisualElement>("Setting_Office");
            
            // 1階を窓に変更
            isChangeWindow = editPanel.Q<Toggle>("IsChangeWindow");
            isChangeWindow.RegisterValueChangedCallback((evt) =>
            {
                officeBuilding.SetUseWindow(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 壁パネルの高さ
            panelHeightSlider = editPanel.Q<Slider>("PanelHeightSlider");
            panelHeightSlider.lowValue = 0.25f;
            panelHeightSlider.highValue = 2.5f;
            
            panelHeightSlider.RegisterValueChangedCallback((evt) =>
            {
                officeBuilding.SetSpandrelHeight(evt.newValue);
                OnUpdated.Invoke();
            });
        }

        public void SetTarget(PlateauSandboxBuilding building)
        {
            officeBuilding.SetParameter(building.officeBuildingParams);
            SetParameterUI();
        }
        
        private void SetParameterUI()
        {
            // パラメータをUIに反映
            isChangeWindow.value = officeBuilding.Parameters.useWindow;
            panelHeightSlider.value = officeBuilding.Parameters.spandrelHeight;
        }

        public void Show(bool isShow)
        {
            editPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}