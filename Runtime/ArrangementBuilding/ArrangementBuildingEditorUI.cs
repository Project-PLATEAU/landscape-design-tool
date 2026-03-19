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
        public const string UIEditBuildingArea = "EditBuildingArea";
        public const string UIHeightContainer = "HeightContainer";
        public const string UIHeightSlider = "HeightSlider";
        public const string UIWidthSlider = "WidthSlider";
        public const string UIDepthSlider = "DepthSlider";

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
            editPanel = element.Q<VisualElement>(UIEditBuildingArea);
            heightContainer = editPanel.Q<VisualElement>(UIHeightContainer);
            heightSlider = editPanel.Q<SliderInt>(UIHeightSlider);
            widthSlider = editPanel.Q<SliderInt>(UIWidthSlider);
            depthSlider = editPanel.Q<SliderInt>(UIDepthSlider);

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

        private void RegisterSliderAction()
        {
            heightSlider.RegisterValueChangedCallback(OnHeightSliderChanged);
            widthSlider.RegisterValueChangedCallback(OnWidthSliderChanged);
            depthSlider.RegisterValueChangedCallback(OnDepthSliderChanged);
        }

        private void UnregisterSliderAction()
        {
            heightSlider.UnregisterValueChangedCallback(OnHeightSliderChanged);
            widthSlider.UnregisterValueChangedCallback(OnWidthSliderChanged);
            depthSlider.UnregisterValueChangedCallback(OnDepthSliderChanged);
        }

        public void ShowPanel(bool isShow)
        {
            editPanel.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;

            if (isShow)
            {
                RegisterSliderAction();
            }
            else
            {
                UnregisterSliderAction();
            }
        }

        private void OnHeightSliderChanged(ChangeEvent<int> e)
        {
            arrangementBuildingEditor.SetHeight(e.newValue);
            UpdateBuildingMesh();
        }

        private void OnWidthSliderChanged(ChangeEvent<int> e)
        {
            arrangementBuildingEditor.SetWidth(e.newValue);
            UpdateBuildingMesh();
        }

        private void OnDepthSliderChanged(ChangeEvent<int> e)
        {
            arrangementBuildingEditor.SetDepth(e.newValue);
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
                if (!CanShowPanel(building))
                {
                    ShowPanel(false);
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

        private bool CanShowPanel(PlateauSandboxBuilding building)
        {
            // サイズ変更パネルを表示する場合は構造物設定パネルを表示しない
            return !building.isSizePanelVisible;
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

            depthSlider.value = (int)arrangementBuildingEditor.GetDepth();
            depthSlider.lowValue = (int)arrangementBuildingEditor.GetMinAndMaxDepth().min;
            depthSlider.highValue = (int)arrangementBuildingEditor.GetMinAndMaxDepth().high;
        }
    }
}