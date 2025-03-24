using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Landscape2.Runtime;

namespace Landscape2.Runtime.BuildingEditor
{
    /// <summary>
    /// 読み込んだ建物編集データを管理するクラス
    /// </summary>
    public sealed class BuildingsDataComponent
    {
        // 建物編集データリスト
        private static readonly List<BuildingProperty> properties = new List<BuildingProperty>();
        // 建物編集がロードされた際のイベント
        public static event Action BuildingDataLoaded = delegate { };
        // 建物編集が削除された際のイベント
        public static event Action<List<BuildingProperty>, string> BuildingDataDeleted = delegate { };

        /// <summary>
        /// 建物編集データを新規に追加するメソッド
        /// </summary>
        public static void AddNewProperty(BuildingProperty newProperty)
        {
            properties.Add(newProperty);
            
            // プロジェクトに通知
            ProjectSaveDataManager.Add(ProjectSaveDataType.EditBuilding, newProperty.ID);
        }

        /// <summary>
        /// セーブデータを基に建物編集データを新規に追加するメソッド
        /// </summary>
        public static void AddAllNewProperty(List<BuildingProperty> newProperties)
        {
            foreach (var newProperty in newProperties)
            {
                AddNewProperty(newProperty);
            }
            // 建物編集データがロードされたことを通知
            BuildingDataLoaded();
        }

        public static void DeleteProperty(List<BuildingProperty> deleteProperties, string projectID)
        {
            BuildingDataDeleted(deleteProperties, projectID);
            
            // 通知してから削除
            foreach (var deleteProperty in deleteProperties)
            {
                properties.Remove(deleteProperty);
            }
            
            // プロジェクトから削除
            foreach (var deleteProperty in deleteProperties)
            {
                ProjectSaveDataManager.Delete(ProjectSaveDataType.EditBuilding, deleteProperty.ID);
            }
        }

        /// <summary>
        /// 対象の建物編集データを取得するメソッド
        /// </summary>
        public static BuildingProperty GetProperty(int index)
        {
            if (index < 0 || index >= properties.Count) return null;
            return properties[index];
        }
        
        /// <summary>
        /// 建物編集データリストの長さを取得するメソッド
        /// </summary>
        public static int GetPropertyCount()
        {
            return properties.Count;
        }

        public static int GetPropertyCount(string gmlID)
        {
            return properties.Count(p => p.GmlID == gmlID);
        }
        
        public static int GetDeletePropertyCount(string gmlID)
        {
            return properties.Count(p => p.GmlID == gmlID && p.IsDeleted);
        }

        /// <summary>
        /// gmlIDが一致する建物編集データが存在するかを調べるメソッド
        /// </summary>
        public static bool IsContainsProperty(string gmlID)
        {
            return properties.Exists(x => x.GmlID == gmlID && x.IsEditable);
        }

        /// <summary>
        /// 建物編集の変更内容を適用
        /// </summary>
        public static bool TryApplyBuildingEdit(string gmlID, List<Color> colors, List<float> smoothness)
        {
            var index = properties.FindIndex(x => x.GmlID == gmlID && x.IsEditable);
            if (index < 0 || index >= properties.Count) return false;
            
            var property = properties[index];

            // 建物の色とSmoothnessを保存
            property.ColorData = colors;
            property.SmoothnessData = smoothness;
            
            ProjectSaveDataManager.Edit(ProjectSaveDataType.EditBuilding, property.ID);
            
            return true;
        }
        
        public static void SetBuildingEditable(string id, bool isEditable)
        {
            var index = properties.FindIndex(x => x.ID == id);
            if (index < 0 || index >= properties.Count) return;
            
            properties[index].IsEditable = isEditable;
        }
        
        public static void SetBuildingDelete(string gmlID, bool isDelete)
        {
            if (!properties.Any(p => p.GmlID == gmlID && p.IsEditable))
            {
                // 追加
                var newProperty = new BuildingProperty(gmlID, new List<Color>(), new List<float>(), isDelete);
                AddNewProperty(newProperty);
                
                ProjectSaveDataManager.Add(ProjectSaveDataType.EditBuilding, newProperty.ID);
            }
            else
            {
                var property = properties.Find(p => p.GmlID == gmlID && p.IsEditable);
                property.IsDeleted = isDelete;
                
                ProjectSaveDataManager.Edit(ProjectSaveDataType.EditBuilding, property.ID);
            }
        }
        
        public static void LoadProject()
        {
            BuildingDataLoaded();
        }
        
        public static List<BuildingProperty> GetDeleteBuildings()
        {
            return properties.Where(p => p.IsDeleted).ToList();
        }
        
        public static List<BuildingProperty> GetBuildings(string gmlID)
        {
            return properties.Where(p => p.GmlID == gmlID).ToList();
        }
        
        public static int GetDeleteBuildingsCount(string gmlID)
        {
            return properties.Count(p => p.GmlID == gmlID && p.IsDeleted);
        }

        /// <summary>
        /// 建物が編集されているかどうかを判定
        /// </summary>
        public static bool IsCustomEdited(BuildingProperty buildingProperty)
        {
            bool hasCustomColor = buildingProperty.ColorData != null && 
                buildingProperty.ColorData.Any(c => c != BuildingColorEditor.InitialColor);
            bool hasCustomSmoothness = buildingProperty.SmoothnessData != null && 
                buildingProperty.SmoothnessData.Any(s => s != BuildingColorEditor.InitialSmoothness);

            return hasCustomColor || hasCustomSmoothness;
        }

        /// <summary>
        /// 最小レイヤーのデータを取得する共通処理
        /// </summary>
        private static T GetMinLayerData<T>(
            List<ProjectData> projects,
            List<BuildingProperty> buildingProperties,
            Func<BuildingProperty, bool> hasValidData,
            Func<BuildingProperty, T> getValue,
            T defaultValue)
        {
            foreach (var project in projects)
            {
                var property = buildingProperties.FirstOrDefault(p =>
                    ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.EditBuilding, project.projectID, p.ID) &&
                    hasValidData(p));
                
                if (property != null)
                {
                    return getValue(property);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 最小レイヤーの建物プロパティの値を取得
        /// </summary>
        public static (List<Color> colors, List<float> smoothness, bool isDeleted) GetMinLayerPropertyValues(string gmlID, string excludeProjectID = null)
        {
            var projects = ProjectSaveDataManager.ProjectSetting.ProjectList
                .Where(p => p.projectID != excludeProjectID)
                .OrderBy(p => p.layer)
                .ToList();

            // デフォルトのカラーリストを3つの要素で初期化
            var defaultColors = Enumerable.Repeat(BuildingColorEditor.InitialColor, 3).ToList();
            // デフォルトのスムースネスリストを3つの要素で初期化
            var defaultSmoothness = Enumerable.Repeat(BuildingColorEditor.InitialSmoothness, 3).ToList();

            if (!projects.Any())
                return (
                    defaultColors,
                    defaultSmoothness,
                    false
                );

            var buildingProperties = GetBuildings(gmlID);
            if (!buildingProperties.Any())
                return (
                    defaultColors,
                    defaultSmoothness,
                    false
                );

            return (
                GetMinLayerData(
                    projects,
                    buildingProperties,
                    p => p.ColorData != null && p.ColorData.Any(),
                    p => p.ColorData,
                    defaultColors),
                GetMinLayerData(
                    projects,
                    buildingProperties,
                    p => p.SmoothnessData != null && p.SmoothnessData.Any(),
                    p => p.SmoothnessData,
                    defaultSmoothness),
                GetMinLayerData(
                    projects,
                    buildingProperties,
                    p => true,
                    p => p.IsDeleted,
                    false)
            );
        }

        /// <summary>
        /// 指定されたGMLIDに紐づいている建物プロパティのIDを元に、最小レイヤーのプロジェクト名を取得する
        /// </summary>
        public static (string projectID, string projectName) GetLowestLayerProjectNamesByGmlID(string gmlID)
        {
            var buildingProperties = GetBuildings(gmlID);
            var projectNames = new List<(string, string)>();

            foreach (var bp in buildingProperties)
            {
                string targetProjectID = ProjectSaveDataManager.GetProjectID(ProjectSaveDataType.EditBuilding, bp.ID);
                if (string.IsNullOrEmpty(targetProjectID))
                {
                    continue;
                }

                var currentProject = ProjectSaveDataManager.ProjectSetting.GetProject(targetProjectID);
                var otherProjects = ProjectSaveDataManager.ProjectSetting.ProjectList
                    .Where(p => p.projectID != targetProjectID)
                    .ToList();

                if (!otherProjects.Any() || currentProject.layer <= otherProjects.Min(p => p.layer))
                {
                    projectNames.Add((currentProject.projectID, currentProject.projectName));
                }
            }

            return projectNames.Distinct().FirstOrDefault();
        }
    }

    /// <summary>
    /// 建物編集データのプロパティを保持するクラス
    /// </summary>
    public class BuildingProperty
    {
        public string ID { get; private set; }
        public string GmlID { get; private set; }
        public List<Color> ColorData { get; set; }
        public List<float> SmoothnessData { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEditable { get; set; }

        public BuildingProperty(string gmlID, List<Color> colorData, List<float> smoothnessData, bool isDeleted)
        {
            ID = System.Guid.NewGuid().ToString();
            GmlID = gmlID;
            ColorData = colorData;
            SmoothnessData = smoothnessData;
            IsDeleted = isDeleted;
            IsEditable = true;
        }
    }
}
