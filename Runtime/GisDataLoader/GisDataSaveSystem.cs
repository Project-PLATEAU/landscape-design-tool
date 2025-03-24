using Landscape2.Runtime.SaveData;
using System.Collections.Generic;
using System.Linq;
using ToolBox.Serialization;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISポイントのセーブシステム
    /// </summary>
    public class GisDataSaveSystem
    {
        private readonly string saveKey = "GisPoint";
        
        private GisPointInfos gisPointInfos;
        
        public GisDataSaveSystem(SaveSystem saveSystem, GisPointInfos gisPointInfos)
        {
            saveSystem.SaveEvent += OnSave;
            saveSystem.LoadEvent += OnLoad;
            saveSystem.DeleteEvent += OnDelete;
            saveSystem.ProjectChangedEvent += OnProjectChanged;
            
            this.gisPointInfos = gisPointInfos;
        }

        private void OnSave(string projectID)
        {
            var saveData = new List<GisPointInfo>();
            foreach (var gisPointInfo in gisPointInfos.Points)
            {
                if (!string.IsNullOrEmpty(projectID))
                {
                    if (ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.GisData, projectID, gisPointInfo.ID.ToString()))
                    {
                        saveData.Add(gisPointInfo);
                    }
                }
                else
                {
                    saveData.Add(gisPointInfo);
                }
            }
            // データを保存
            DataSerializer.Save(saveKey, saveData);
        }

        private void OnLoad(string projectID)
        {
            // データをロード
            var loadedData = DataSerializer.Load<List<GisPointInfo>>(saveKey);
            gisPointInfos.AddPoints(loadedData, projectID);
        }
        
        private void OnDelete(string projectID)
        {
            // データを削除
            var deleteIds = new List<string>();
            foreach (var gisPointInfo in gisPointInfos.Points)
            {
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.GisData,
                        projectID,
                        gisPointInfo.ID.ToString(),
                        false))
                {
                    deleteIds.Add(gisPointInfo.AttributeID);
                }
            }
            foreach (var id in deleteIds)
            {
                gisPointInfos.Delete(id);
            }
        }
        
        private void OnProjectChanged(string projectID)
        {
            var attributes = new List<(string attributeID, bool isInteractive)>();
            foreach (var gisPointInfo in gisPointInfos.Points)
            {
                if (attributes.Any(attribute => attribute.attributeID == gisPointInfo.AttributeID))
                {
                    continue;
                }
                bool isVisible = ProjectSaveDataManager.TryCheckData(ProjectSaveDataType.GisData, projectID, gisPointInfo.ID.ToString());
                attributes.Add((gisPointInfo.AttributeID, isVisible));
            }
            
            // 操作調整
            foreach (var (attributeID, isEditable) in attributes)
            {
                gisPointInfos.SetEditable(attributeID, isEditable);
            }
        }
    }
}