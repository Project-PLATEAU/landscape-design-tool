using Landscape2.Runtime.SaveData;
using System;
using System.Linq;
using UnityEngine.Events;

namespace Landscape2.Runtime
{
    public static class ProjectSaveDataManager
    {
        // プロジェクト設定
        public static ProjectSetting ProjectSetting = new();
        
        // レイヤー変更イベント
        public static UnityEvent<int> OnLayerChanged = new();
        
        // 配置アセット
        private static ProjectSaveData_Asset SaveDataAsset = new();
        
        // カメラ視点一覧
        private static ProjectSaveData_CameraPosition SaveDataCameraPosition = new();
        
        // 色彩編集した建築物一覧
        private static ProjectSaveData_EditBuilding SaveDataEditBuilding = new();
        
        // GISデータ一覧
        private static ProjectSaveData_GisData SaveDataGisData = new();
        
        // 景観計画区域一覧
        private static ProjectSaveData_LandscapePlan SaveDataLandscapePlan = new();
        
        // 見通し解析一覧
        private static ProjectSaveData_LineOfSight SaveDataLineOfSight = new();
        
        // BIMインポートして配置した建物一覧
        private static ProjectSaveData_BimImport SaveDataBimImport = new();
        
        
        // 編集イベント
        public static UnityEvent<string> OnEditProject = new();

        /// <summary>
        /// レイヤーを設定
        /// </summary>
        public static void SetLayer(string projectID, int layer)
        {
            var project = ProjectSetting.GetProject(projectID);
            project.layer = layer;
            OnLayerChanged.Invoke(layer);
        }
        
        /// <summary>
        /// プロジェクトに追加
        /// </summary>
        public static void Add(ProjectSaveDataType dataType, string id, string projectID = "", bool isEdit = true)
        {
            projectID = string.IsNullOrEmpty(projectID) ? ProjectSetting.CurrentProject.projectID : projectID;
            
            switch (dataType)
            {
                case ProjectSaveDataType.Asset:
                    SaveDataAsset.Add(projectID, id);
                    break;
                case ProjectSaveDataType.CameraPosition:
                    SaveDataCameraPosition.Add(projectID, id);
                    break;
                case ProjectSaveDataType.EditBuilding:
                    SaveDataEditBuilding.Add(projectID, id);
                    break;
                case ProjectSaveDataType.GisData:
                    SaveDataGisData.Add(projectID, id);
                    break;
                case ProjectSaveDataType.LandscapePlan:
                    SaveDataLandscapePlan.Add(projectID, id);
                    break;
                case ProjectSaveDataType.LineOfSight:
                    SaveDataLineOfSight.Add(projectID, id);
                    break;
                case ProjectSaveDataType.BimImport:
                    SaveDataBimImport.Add(projectID, id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            if (isEdit)
            {
                Edit(dataType, id);
            }
        }

        /// <summary>
        /// 現在のプロジェクトから削除
        /// </summary>
        public static void Delete(ProjectSaveDataType dataType, string id)
        {
            Edit(dataType, id);
            switch (dataType)
            {
                case ProjectSaveDataType.Asset:
                    SaveDataAsset.Delete(id);
                    break;
                case ProjectSaveDataType.CameraPosition:
                    SaveDataCameraPosition.Delete(id);
                    break;
                case ProjectSaveDataType.EditBuilding:
                    SaveDataEditBuilding.Delete(id);
                    break;
                case ProjectSaveDataType.GisData:
                    SaveDataGisData.Delete(id);
                    break;
                case ProjectSaveDataType.LandscapePlan:
                    SaveDataLandscapePlan.Delete(id);
                    break;
                case ProjectSaveDataType.LineOfSight:
                    SaveDataLineOfSight.Delete(id);
                    break;
                case ProjectSaveDataType.BimImport:
                    SaveDataBimImport.Delete(id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        /// <summary>
        /// 編集中に
        /// </summary>
        public static void Edit(ProjectSaveDataType dataType, string id)
        {
            var projectID = GetProjectID(dataType, id);
            
            // プロジェクトデータを編集中に
            OnEditProject.Invoke(projectID);
        }

        public static string GetProjectID(ProjectSaveDataType dataType, string id)
        {
            return dataType switch
            {
                ProjectSaveDataType.Asset => SaveDataAsset.GetProjectID(id),
                ProjectSaveDataType.CameraPosition => SaveDataCameraPosition.GetProjectID(id),
                ProjectSaveDataType.EditBuilding => SaveDataEditBuilding.GetProjectID(id),
                ProjectSaveDataType.GisData => SaveDataGisData.GetProjectID(id),
                ProjectSaveDataType.LandscapePlan => SaveDataLandscapePlan.GetProjectID(id),
                ProjectSaveDataType.LineOfSight => SaveDataLineOfSight.GetProjectID(id),
                ProjectSaveDataType.BimImport => SaveDataBimImport.GetProjectID(id),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
            };
        }

        /// <summary>
        /// IDが存在するか確認
        /// </summary>
        public static bool TryCheckData(ProjectSaveDataType dataType, string projectID, string id, bool isCheckEditMode = true)
        {
            if (isCheckEditMode && !ProjectSetting.IsEditMode)
            {
                // 閲覧モードであればチェックを通さない
                return false;
            }

            return dataType switch
            {
                ProjectSaveDataType.Asset => SaveDataAsset.TryCheckData(projectID, id),
                ProjectSaveDataType.CameraPosition => SaveDataCameraPosition.TryCheckData(projectID, id),
                ProjectSaveDataType.EditBuilding => SaveDataEditBuilding.TryCheckData(projectID, id),
                ProjectSaveDataType.GisData => SaveDataGisData.TryCheckData(projectID, id),
                ProjectSaveDataType.LandscapePlan => SaveDataLandscapePlan.TryCheckData(projectID, id),
                ProjectSaveDataType.LineOfSight => SaveDataLineOfSight.TryCheckData(projectID, id),
                ProjectSaveDataType.BimImport => SaveDataBimImport.TryCheckData(projectID, id),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
            };
        }

        /// <summary>
        /// 指定されたデータが表示可能かどうかをレイヤー値で判定
        /// </summary>
        public static bool IsVisibleByLayer(ProjectSaveDataType dataType, string id)
        {
            string targetProjectID = GetProjectID(dataType, id);
            if (string.IsNullOrEmpty(targetProjectID))
            {
                return false;
            }

            var currentProject = ProjectSetting.GetProject(targetProjectID);
            var otherProjects = ProjectSetting.ProjectList
                .Where(p => p.projectID != targetProjectID)
                .ToList();

            // 他のプロジェクトが存在しない場合は表示可能
            if (!otherProjects.Any())
            {
                return true;
            }

            // 他のプロジェクトの中で最小のレイヤー値を取得
            var minLayerInOthers = otherProjects.Min(p => p.layer);
            
            // 現在のプロジェクトのレイヤー値が、他のプロジェクトの最小値以下なら表示可能
            return currentProject.layer <= minLayerInOthers;
        }

        /// <summary>
        /// 現在のプロジェクトが最小レイヤーかどうかをチェックする
        /// </summary>
        public static bool IsCurrentProjectMinLayer()
        {
            var currentProject = ProjectSetting.CurrentProject;
            var projects = ProjectSetting.ProjectList.OrderBy(p => p.layer).ToList();
            return projects.Any() && currentProject.layer == projects.First().layer;
        }

        /// <summary>
        /// 一番下のレイヤーのプロジェクト名を取得する
        /// </summary>
        public static bool TryGetLowestLayerProjectName(out string projectName)
        {
            var projects = ProjectSetting.ProjectList.OrderBy(p => p.layer).ToList();
            if (projects.Any())
            {
                projectName = projects.First().projectName;
                return true;
            }
            projectName = null;
            return false;
        }
    }
}