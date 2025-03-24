using System.Collections.Generic;
using UnityEngine;
using ToolBox.Serialization;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観区画の保存と読み込み時の処理を管理するクラス
    /// </summary>
    public static class LandscapePlanSaveSystem
    {
        static public void SetEvent(SaveSystem saveSystem)
        {
            saveSystem.SaveEvent += SaveInfo;
            saveSystem.LoadEvent += LoadInfo;
            saveSystem.DeleteEvent += DeleteInfo;
            saveSystem.ProjectChangedEvent += SetProjectInfo;
        }

        /// <summary>
        /// 景観計画のデータセーブの処理
        /// </summary>
        static void SaveInfo(string projectID)
        {
            // セーブデータ用クラスに現在の区画データをコピー
            List<PlanAreaSaveData> planAreaSaveDatas = new List<PlanAreaSaveData>();
            int areaDataCount = AreasDataComponent.GetPropertyCount();
            for (int i = 0; i < areaDataCount; i++)
            {
                AreaProperty areaProperty = AreasDataComponent.GetProperty(i);

                if (!string.IsNullOrEmpty(projectID))
                {
                    if (!ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.LandscapePlan, projectID, areaProperty.ID.ToString()))
                    {
                        // プロジェクトに該当しなければ保存しない
                        continue;
                    }
                }

                PlanAreaSaveData saveData = new PlanAreaSaveData(
                    areaProperty.ID,
                    areaProperty.Name,
                    areaProperty.LimitHeight,
                    areaProperty.LineOffset,
                    areaProperty.Color,
                    areaProperty.WallMaxHeight,
                    areaProperty.PointData
                    );

                planAreaSaveDatas.Add(saveData);
            }

            // データを保存
            DataSerializer.Save("PlanAreas", planAreaSaveDatas);
        }

        /// <summary>
        /// 景観計画のデータロードと生成の処理
        /// </summary>
        static void LoadInfo(string projectID)
        {
            // 景観区画のセーブデータをロード
            List<PlanAreaSaveData> loadedPlanAreaDatas = DataSerializer.Load<List<PlanAreaSaveData>>("PlanAreas");

            if (loadedPlanAreaDatas != null)
            {
                // ロードした頂点座標データからMeshを生成
                LandscapePlanLoadManager landscapePlanLoadManager = new LandscapePlanLoadManager();
                var loadedProperties = landscapePlanLoadManager.LoadFromSaveData(loadedPlanAreaDatas);
                
                foreach (var loadedProperty in loadedProperties)
                {
                    // プロジェクトへ保存
                    ProjectSaveDataManager.Add(ProjectSaveDataType.LandscapePlan, loadedProperty.ID.ToString(), projectID, false);
                }
            }
            else
            {
                Debug.LogError("No saved project data found.");
            }
        }
        
        /// <summary>
        /// 削除通知
        /// </summary>
        /// <param name="projectID"></param>
        static void DeleteInfo(string projectID)
        {
            var deleteList = new List<AreaProperty>();
            int areaDataCount = AreasDataComponent.GetPropertyCount();
            for (int i = 0; i < areaDataCount; i++)
            {
                var areaProperty = AreasDataComponent.GetProperty(i);
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.LandscapePlan,
                        projectID,
                        areaProperty.ID.ToString(),
                        false))
                {
                    deleteList.Add(areaProperty);
                }
            }
            foreach (var areaProperty in deleteList)
            {
                AreasDataComponent.TryRemoveProperty(areaProperty);
            }
        }
        
        
        /// <summary>
        /// プロジェクト更新通知
        /// </summary>
        /// <param name="projectID"></param>
        static void SetProjectInfo(string projectID)
        {
            int areaDataCount = AreasDataComponent.GetPropertyCount();
            for (int i = 0; i < areaDataCount; i++)
            {
                var areaProperty = AreasDataComponent.GetProperty(i);
                if (ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.LandscapePlan, projectID, areaProperty.ID.ToString()))
                {
                    areaProperty.SetIsEditable(true);
                }
                else
                {
                    areaProperty.SetIsEditable(false);
                }
            }

            // エリア数の変更を通知して画面を更新する
            AreasDataComponent.InvokeAreaCountChanged();
        }
    }
}
