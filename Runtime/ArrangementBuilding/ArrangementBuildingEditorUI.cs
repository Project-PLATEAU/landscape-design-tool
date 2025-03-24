using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 配置ビルのUIを提供するクラス
    /// </summary>
    public class ArrangementBuildingEditorUI
    {
        private VisualElement editPanel;
        private readonly ArrangementBuildingEditor arrangementBuildingEditor = new();
        
        // 共通パラメータ
        private VisualElement heightContainer;
        private SliderInt heightSlider;
        private SliderInt widthSlider;
        private SliderInt depthSlider;
        
        // 個別パラメータ
        private readonly ArrangementBuildingApartmentUI apartmentUI;
        private readonly ArrangementBuildingConvenienceStoreUI convenienceStoreUI;
        private readonly ArrangementBuildingHouseUI houseUI;
        private readonly ArrangementBuildingOfficeBuildingUI officeBuildingUI;
        
        public ArrangementBuildingEditorUI(VisualElement element)
        {
            editPanel = element.Q<VisualElement>("EditBuildingArea");
            RegisterEditButtonAction();
            
            // 個別パラメータ
            apartmentUI = new ArrangementBuildingApartmentUI(editPanel);
            apartmentUI.OnUpdated.AddListener(() => arrangementBuildingEditor.ApplyBuildingMesh());
            
            convenienceStoreUI = new ArrangementBuildingConvenienceStoreUI(editPanel);
            convenienceStoreUI.OnUpdated.AddListener(() => arrangementBuildingEditor.ApplyBuildingMesh());
            
            houseUI = new ArrangementBuildingHouseUI(editPanel);
            houseUI.OnUpdated.AddListener(() => arrangementBuildingEditor.ApplyBuildingMesh());
            
            officeBuildingUI = new ArrangementBuildingOfficeBuildingUI(editPanel);
            officeBuildingUI.OnUpdated.AddListener(() => arrangementBuildingEditor.ApplyBuildingMesh());

            // デフォルト非表示
            ShowPanel(false);
        }
        
        private void RegisterEditButtonAction()
        {
            heightContainer = editPanel.Q<VisualElement>("HeightContainer");
            heightSlider = editPanel.Q<SliderInt>("HeightSlider");
            heightSlider.RegisterValueChangedCallback((evt) => OnHeightSliderChanged(evt.newValue));
            
            widthSlider = editPanel.Q<SliderInt>("WidthSlider");
            widthSlider.RegisterValueChangedCallback((evt) => OnWidthSliderChanged(evt.newValue));
            
            depthSlider = editPanel.Q<SliderInt>("DepthSlider");
            depthSlider.RegisterValueChangedCallback((evt) => OnDepthSliderChanged(evt.newValue));
        }
        
        public void ShowPanel(bool isShow)
        {
            editPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void OnHeightSliderChanged(float value)
        {
            arrangementBuildingEditor.SetHeight(value);
            UpdateBuildingMesh();
        }
        
        private void OnWidthSliderChanged(float value)
        {
            arrangementBuildingEditor.SetWidth(value);
            UpdateBuildingMesh();
        }
        
        private void OnDepthSliderChanged(float value)
        {
            arrangementBuildingEditor.SetDepth(value);
            UpdateBuildingMesh();
        }
        
        private void UpdateBuildingMesh()
        {
            arrangementBuildingEditor.ApplyBuildingMesh();
        }

        public void TryShowPanel(GameObject selectObject)
        {
            if (selectObject.TryGetComponent<PlateauSandboxBuilding>(out var building))
            {
                // 特定の建物はパネルを表示しない
                if (!CanShowPanel(building.name))
                {
                    return;
                }
                
                // 共通パラメータの表示
                arrangementBuildingEditor.SetTarget(building);
                InitializeSliders();
                ShowPanel(true);
                
                // 個別パラメータを非表示
                apartmentUI.Show(false);
                officeBuildingUI.Show(false);
                houseUI.Show(false);
                convenienceStoreUI.Show(false);
                
                // 個別パラメータの表示
                switch (building.buildingType)
                {
                    case BuildingType.k_Apartment:
                        apartmentUI.SetTarget(building);
                        apartmentUI.Show(true);
                        break;
                    case BuildingType.k_OfficeBuilding:
                        officeBuildingUI.SetTarget(building);
                        officeBuildingUI.Show(true);        
                        break;
                    case BuildingType.k_House:
                        houseUI.SetTarget(building);
                        houseUI.Show(true);
                        break;
                    case BuildingType.k_ConvenienceStore:
                        convenienceStoreUI.SetTarget(building);
                        convenienceStoreUI.Show(true);
                        break;
                    case BuildingType.k_CommercialBuilding:
                    case BuildingType.k_Hotel:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                return;
            }
            ShowPanel(false);
        }

        private bool CanShowPanel(String buildingName)
        {
            // TODO: 名前ではなく特定のパラメータで判定したい
            return !buildingName.Contains("KotoHouse");
        }

        private void InitializeSliders()
        {
            if (arrangementBuildingEditor.CanSlideHeight())
            {
                heightContainer.style.display = DisplayStyle.Flex;
                heightSlider.value = (int)arrangementBuildingEditor.GetHeight();
                heightSlider.lowValue = (int)arrangementBuildingEditor.GetMinAndMaxHeight().min;
                heightSlider.highValue = (int)arrangementBuildingEditor.GetMinAndMaxHeight().high;
            }
            else
            {
                heightContainer.style.display = DisplayStyle.None;
            }

            widthSlider.value = (int)arrangementBuildingEditor.GetWidth();
            widthSlider.lowValue = (int)arrangementBuildingEditor.GetMinAndMaxWidth().min;
            widthSlider.highValue = (int)arrangementBuildingEditor.GetMinAndMaxWidth().high;
            
            depthSlider.value =  (int)arrangementBuildingEditor.GetDepth();
            depthSlider.lowValue = (int)arrangementBuildingEditor.GetMinAndMaxDepth().min;
            depthSlider.highValue = (int)arrangementBuildingEditor.GetMinAndMaxDepth().high;
        }
    }
}