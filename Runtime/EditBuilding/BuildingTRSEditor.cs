using Landscape2.Runtime.BuildingEditor;
using Landscape2.Runtime.Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BuildingTRSEditor : ISubComponent
    {
        const float focusDistanceMultiplyer = 16f;
        private EditMode editMode = new();

        BuildingTRSEditorUI trsUI;
        BuildingDeleteListUI deleteListUI;
        GameObject target;

        public BuildingTRSEditor(EditBuilding editBuilding, VisualElement element, LandscapeCamera landscapeCamera)
        {
            editMode.OnCancel();
            trsUI = new(editBuilding, element);
            deleteListUI = new(element, this);

            var assetFocus = new GameObjectFocus(landscapeCamera);
            assetFocus.focusFinishCallback += _ => assetFocus.FocusFinish();

            GameObject targetViewObject = new()
            {
                name = "TargetViewObject"
            };
            editBuilding.OnBuildingSelected += OnSelectBuilding;

            deleteListUI.OnClickShowButton += (go) =>
            {
                // Debug.Log($"go => {go.name} : {go.transform.position} : {go.transform.localPosition}", go);
                editBuilding.SetTargetObject(go);
                var bounds = RendererUtil.CalculateBounds(go);
                targetViewObject.transform.position = bounds.center;
                assetFocus.Focus(targetViewObject.transform, focusDistanceMultiplyer);

                var gml = CityObjectUtil.GetGmlID(go);
                if (BuildingsDataComponent.GetDeleteBuildingsCount(gml) == 1)
                {
                    // １つだけならば表示
                    var editComponent = BuildingTRSEditingComponent.TryGetOrCreate(go);
                    editComponent.ShowBuilding(true);
                }
                BuildingsDataComponent.SetBuildingDelete(gml, false);
            };

            deleteListUI.OnClickListElement += (go) =>
            {
                editBuilding.SetTargetObject(go);
                var bounds = RendererUtil.CalculateBounds(go);
                targetViewObject.transform.position = bounds.center;
                assetFocus.Focus(targetViewObject.transform, focusDistanceMultiplyer);
            };

            trsUI.OnClickDeleteButton += () =>
            {
                if (target == null)
                {
                    Debug.LogWarning($"targetがないです");
                    return;
                }

                if (!target.TryGetComponent<Renderer>(out var r))
                {
                    Debug.LogWarning($"{target.name} : rendererがnullです");
                }

                var editComponent = BuildingTRSEditingComponent.TryGetOrCreate(r.gameObject);
                editComponent.ShowBuilding(false);

                var gml = CityObjectUtil.GetGmlID(target);
                if (BuildingsDataComponent.GetDeleteBuildingsCount(gml) > 0)
                {
                    return;
                }

                deleteListUI?.AppendList(r.gameObject, true);

                BuildingsDataComponent.SetBuildingDelete(gml, true);
            };
            trsUI.OnClickTransButton += () =>
            {
                ChangeEditMode(target, TransformType.Position);
            };
            trsUI.OnClickRotateButton += () =>
            {
                ChangeEditMode(target, TransformType.Rotation);
            };
            trsUI.OnClickScaleButton += () =>
            {
                ChangeEditMode(target, TransformType.Scale);
            };
            
            BuildingsDataComponent.BuildingDataLoaded += OnLoadBuildings;
            BuildingsDataComponent.BuildingDataDeleted += OnDeleteBuildings;
        }

        void ChangeEditMode(GameObject target, TransformType type)
        {
            editMode.OnCancel();
            //  後で使用するかも知れないので一旦コメントアウトのみ
            // editMode.CreateRuntimeHandle(target, type);
        }

        public void OnSelectBuilding(GameObject select, bool canEdit)
        {
            if (!canEdit)
            {
                return;
            }

            if (select != target)
            {
                if (select != null)
                {
                    ChangeEditMode(select, TransformType.Scale);
                }
            }
            target = select;
        }

        public void OnDisable()
        {
            trsUI?.OnDisable();
            deleteListUI?.OnDisable();
            target = null;
        }

        public void OnEnable()
        {
            trsUI?.OnEnable();
            deleteListUI?.OnEnable();
        }

        public void Update(float deltaTime)
        {
            trsUI?.Update(deltaTime);
            
            deleteListUI?.ShowListEmpty(
                BuildingsDataComponent.GetDeleteBuildings().Count <= 0);
        }

        public void Start()
        {
            trsUI?.Start();
            deleteListUI?.Start();
        }

        public void LateUpdate(float deltaTime)
        {
        }

        private void OnLoadBuildings()
        {
            var count = BuildingsDataComponent.GetPropertyCount();
            
            // 全てセットし直す
            for (int i = 0; i < count; i++)
            {
                var buildingProperty = BuildingsDataComponent.GetProperty(i);
                ShowBuilding(!buildingProperty.IsDeleted, buildingProperty);
            }
        }

        private void OnDeleteBuildings(List<BuildingProperty> deleteBuildings, string projectID)
        {
            foreach (var deleteBuilding in deleteBuildings)
            {
                if (BuildingsDataComponent.GetDeletePropertyCount(deleteBuilding.GmlID) > 1)
                {
                    // 他にも同じGmlIDの建物がある場合はスキップ
                    continue;
                }

                ShowBuilding(true, deleteBuilding);
                
                var cityObjectGroup = CityModelHandler.GetCityObjectGroup(deleteBuilding.GmlID);
                if (cityObjectGroup == null)
                {
                    continue;
                }
                // レイヤーを戻しておく
                LayerMaskUtil.SetIgnore(cityObjectGroup.gameObject, false);
            }
        }
        
        private void ShowBuilding(bool isVisible, BuildingProperty property)
        {
            var building = CityModelHandler.GetCityObjectGroup(property.GmlID);
            if (!building)
            {
                return;
            }
            var editComponent = BuildingTRSEditingComponent.TryGetOrCreate(building.gameObject);
            editComponent.ShowBuilding(isVisible);
        }
    }
}
