using Landscape2.Runtime.Common;
using PLATEAU.CityInfo;
using System.Collections.Generic;
using System.Linq;
using ToolBox.Serialization;
using UnityEngine;

namespace Landscape2.Runtime.BuildingEditor
{
    /// <summary>
    /// 建物編集の保存と読み込み時の処理を管理するクラス
    /// </summary>   
    public class BuildingSaveLoadSystem
    {
        public void SetEvent(SaveSystem saveSystem)
        {
            saveSystem.SaveEvent += SaveInfo;
            saveSystem.LoadEvent += LoadInfo;
            saveSystem.DeleteEvent += DeleteInfo;
            saveSystem.ProjectChangedEvent += SetProjectInfo;
            saveSystem.LayerChangedEvent += OnLayerChanged;
        }

        /// <summary>
        /// 景観計画のデータセーブの処理
        /// </summary>
        public void SaveInfo(string projectID)
        {
            // セーブデータ用クラスに現在の建物編集データをコピー
            List<BuildingSaveData> buildingSaveDatas = new List<BuildingSaveData>();

            // プロジェクトIDが指定されている場合は、そのプロジェクトのデータのみを保存
            if (!string.IsNullOrEmpty(projectID))
            {
                int buildingDataCount = BuildingsDataComponent.GetPropertyCount();
                for (int i = 0; i < buildingDataCount; i++)
                {
                    BuildingProperty buildingProperty = BuildingsDataComponent.GetProperty(i);
                    if (!ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.EditBuilding, projectID, buildingProperty.ID))
                    {
                        continue;
                    }

                    var saveData = new BuildingSaveData(
                        buildingProperty.GmlID,
                        buildingProperty.ColorData,
                        buildingProperty.SmoothnessData,
                        buildingProperty.IsDeleted
                    );
                    buildingSaveDatas.Add(saveData);
                }
            }
            else
            {
                int buildingDataCount = BuildingsDataComponent.GetPropertyCount();
                for (int i = 0; i < buildingDataCount; i++)
                {
                    BuildingProperty buildingProperty = BuildingsDataComponent.GetProperty(i);

                    var minLayerValuesResult = BuildingsDataComponent.GetMinLayerPropertyValues(buildingProperty.GmlID);
                    var saveData = new BuildingSaveData(
                        buildingProperty.GmlID,
                        minLayerValuesResult.colors,
                        minLayerValuesResult.smoothness,
                        minLayerValuesResult.isDeleted
                    );
                    buildingSaveDatas.Add(saveData);
                }
            }

            // データを保存
            DataSerializer.Save("Buildings", buildingSaveDatas);
        }

        /// <summary>
        /// 景観計画のデータロードの処理
        /// </summary>
        public void LoadInfo(string projectID)
        {
            // 建物編集のセーブデータをロード
            List<BuildingSaveData> loadedBuildingDatas = DataSerializer.Load<List<BuildingSaveData>>("Buildings");

            if (loadedBuildingDatas != null)
            {
                var addBuildingProperty = loadedBuildingDatas
                    .Select(data => new BuildingProperty(
                        data.GmlID,
                        data.ColorData,
                        data.SmoothnessData,
                        data.IsDeleted))
                    .ToList();
                
                BuildingsDataComponent.AddAllNewProperty(addBuildingProperty);
                
                // プロジェクトに通知
                foreach (var buildingProperty in addBuildingProperty)
                {
                    ProjectSaveDataManager.Add(ProjectSaveDataType.EditBuilding, buildingProperty.ID, projectID, false);
                }
            }
            else
            {
                Debug.LogWarning("No saved project data found.");
            }
        }
        
        private void DeleteInfo(string projectID)
        {
            int buildingDataCount = BuildingsDataComponent.GetPropertyCount();
            var deleteBuildingProperty = new List<BuildingProperty>();
            for (int i = 0; i < buildingDataCount; i++)
            {
                var buildingProperty = BuildingsDataComponent.GetProperty(i);
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.EditBuilding,
                        projectID,
                        buildingProperty.ID,
                        false))
                {
                    deleteBuildingProperty.Add(buildingProperty);
                }
            }
            
            BuildingsDataComponent.DeleteProperty(deleteBuildingProperty, projectID);
        }
        
        private void SetProjectInfo(string projectID)
        {
            int buildingDataCount = BuildingsDataComponent.GetPropertyCount();
            for (int i = 0; i < buildingDataCount; i++)
            {
                var buildingProperty = BuildingsDataComponent.GetProperty(i);
                var isVisible = ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.EditBuilding, projectID, buildingProperty.ID);
                
                BuildingsDataComponent.SetBuildingEditable(buildingProperty.ID, isVisible);
            }
            // 通知
            BuildingsDataComponent.LoadProject();
        }

        private void OnLayerChanged()
        {
            string currentProjectID = ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID;
            SetProjectInfo(currentProjectID);
        }
    }
}
