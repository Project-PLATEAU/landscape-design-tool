using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingConvenienceStoreUI : IArrangementBuildingParameterUI
    {
        private ArrangementBuildingConvenienceStore convenienceStore;
        public UnityEvent OnUpdated = new UnityEvent();
        private VisualElement editPanel;
        private Toggle isSideWall;
        private Slider roofThickness;
        
        public ArrangementBuildingConvenienceStoreUI(VisualElement element)
        {
            convenienceStore = new ArrangementBuildingConvenienceStore();
            RegisterButtons(element);
        }
        
        private void RegisterButtons(VisualElement element)
        {
            editPanel = element.Q<VisualElement>("Setting_Store");
            
            // 側面を壁に設定
            isSideWall = editPanel.Q<Toggle>("IsSideWall");
            isSideWall.RegisterValueChangedCallback((evt) =>
            {
                convenienceStore.SetIsSideWall(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 屋根の厚さ
            roofThickness = editPanel.Q<Slider>("SideWallThickness");
            roofThickness.RegisterValueChangedCallback((evt) =>
            {
                convenienceStore.SetRoofThickness(evt.newValue);
                OnUpdated.Invoke();
            });
        }
        
        public void SetTarget(PlateauSandboxBuilding building)
        {
            convenienceStore.SetParameter(building.conveniParams);
            SetParameterUI();
        }

        private void SetParameterUI()
        {
            // パラメータをUIに反映
            isSideWall.value = convenienceStore.Parameters.isSideWall;
            roofThickness.value = convenienceStore.Parameters.roofThickness;
        }

        public void Show(bool isShow)
        {
            editPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}