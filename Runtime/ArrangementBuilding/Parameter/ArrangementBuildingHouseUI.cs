using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using System;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingHouseUI : IArrangementBuildingParameterUI
    {
        public UnityEvent OnUpdated = new UnityEvent();
        private VisualElement editPanel;
        
        private ArrangementBuildingHouse house;
        private SliderInt floorSlider;
        private Toggle isMakeEaves;
        private DropdownField roofType;
        private Slider roofThickness;
        
        public ArrangementBuildingHouseUI(VisualElement element)
        {
            house = new ArrangementBuildingHouse();
            RegisterButtons(element);
        }
        
        private void RegisterButtons(VisualElement element)
        {
            editPanel = element.Q<VisualElement>("Setting_House");
            
            // 階数
            floorSlider = editPanel.Q<SliderInt>("FloorSlider");
            floorSlider.lowValue = 1;
            floorSlider.highValue = 3;
            floorSlider.RegisterValueChangedCallback((evt) =>
            {
                house.SetNumFloor(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // エントランスに庇を追加
            isMakeEaves = editPanel.Q<Toggle>("IsMakeEaves");
            isMakeEaves.RegisterValueChangedCallback((evt) =>
            {
                house.SetHasEntranceRoof(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 屋根タイプ
            roofType = editPanel.Q<DropdownField>("RoofType");
            foreach (RoofType choice  in Enum.GetValues(typeof(RoofType)))
            {
                switch (choice)
                {
                    case RoofType.flat:
                        roofType.choices.Add("平屋根");
                        break;
                    case RoofType.hipped:
                        roofType.choices.Add("寄棟屋根");
                        break;
                }
            }
            roofType.RegisterValueChangedCallback((evt) =>
            {
                house.SetRoofType(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 屋根の厚さ
            roofThickness = editPanel.Q<Slider>("RoofThickness");
            roofThickness.RegisterValueChangedCallback((evt) =>
            {
                house.SetRoofThickness(evt.newValue);
                OnUpdated.Invoke();
            });
        }
        
        public void SetTarget(PlateauSandboxBuilding building)
        {
            house.SetParameter(building.residenceParams);
            SetParameterUI();
        }
        
        private void SetParameterUI()
        {
            // パラメータをUIに反映
            floorSlider.value = house.Parameters.numFloor;
            isMakeEaves.value = house.Parameters.hasEntranceRoof;
            roofType.value = house.GetRoofTypeString(house.Parameters.roofType);
            roofThickness.value = house.Parameters.roofThickness;
        }

        public void Show(bool isShow)
        {
            editPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}