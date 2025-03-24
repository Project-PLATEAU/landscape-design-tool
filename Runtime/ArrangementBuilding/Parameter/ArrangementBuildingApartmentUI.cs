using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingApartmentUI : IArrangementBuildingParameterUI
    {
        private ArrangementBuildingApartment apartment;
        public UnityEvent OnUpdated = new UnityEvent();
        private VisualElement editPanel;
        
        private Toggle isExtrude;
        private Toggle isBalconyGlass;
        private Toggle isLeftBalcony;
        private Toggle isRightBalcony;
        private Toggle isFrontBalcony;
        private Toggle isBackBalcony;
        
        public ArrangementBuildingApartmentUI(VisualElement element)
        {
            apartment = new ArrangementBuildingApartment();
            RegisterButtons(element);
        }
        
        private void RegisterButtons(VisualElement element)
        {
            editPanel = element.Q<VisualElement>("Setting_Mansion");
            
            // 外側にせり出す
            isExtrude = editPanel.Q<Toggle>("IsExtrude");
            isExtrude.RegisterValueChangedCallback((evt) =>
            {
                apartment.SetConvexBalcony(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 窓ガラスバルコニーに切り替え
            isBalconyGlass = editPanel.Q<Toggle>("IsChangeBalcony");
            isBalconyGlass.RegisterValueChangedCallback((evt) =>
            {
                apartment.SetHasBalconyGlass(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 左側にバルコニーを作成
            isLeftBalcony = editPanel.Q<Toggle>("IsMakeValcony_Left");
            isLeftBalcony.RegisterValueChangedCallback((evt) =>
            {
                apartment.SetHasBalconyLeft(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 右側にバルコニーを作成
            isRightBalcony = editPanel.Q<Toggle>("IsMakeValcony_Right");
            isRightBalcony.RegisterValueChangedCallback((evt) =>
            {
                apartment.SetHasBalconyRight(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 前側にバルコニーを作成
            isFrontBalcony = editPanel.Q<Toggle>("IsMakeValcony_Front");
            isFrontBalcony.RegisterValueChangedCallback((evt) =>
            {
                apartment.SetHasBalconyFront(evt.newValue);
                OnUpdated.Invoke();
            });
            
            // 後ろ側にバルコニーを作成
            isBackBalcony = editPanel.Q<Toggle>("IsMakeValcony_Back");
            isBackBalcony.RegisterValueChangedCallback((evt) =>
            {
                apartment.SetHasBalconyBack(evt.newValue);
                OnUpdated.Invoke();
            });
        }
        
        public void SetTarget(PlateauSandboxBuilding building)
        {
            apartment.SetParameter(building.skyscraperCondominiumParams);
            SetParameterUI();
        }

        private void SetParameterUI()
        {
            // パラメータをUIに反映
            isExtrude.value = apartment.Parameters.convexBalcony;
            isBalconyGlass.value = apartment.Parameters.hasBalconyGlass;
            isLeftBalcony.value = apartment.Parameters.hasBalconyLeft;
            isRightBalcony.value = apartment.Parameters.hasBalconyRight;
            isFrontBalcony.value = apartment.Parameters.hasBalconyFront;
            isBackBalcony.value = apartment.Parameters.hasBalconyBack;
        }

        public void Show(bool isShow)
        {
            editPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}