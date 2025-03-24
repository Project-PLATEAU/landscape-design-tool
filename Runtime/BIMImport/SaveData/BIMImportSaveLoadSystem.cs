using System.Collections.Generic;
using System.Linq;
using ToolBox.Serialization;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class BIMImportSaveLoadSystem
    {
        List<BIMImportSaveData> saveDatas = new();

        public List<BIMImportSaveData> SaveDataList => saveDatas;

        public System.Action<List<BIMImportSaveData>> loadCallback = new(_ => { });
        public System.Action<List<BIMImportSaveData>> deleteCallback = new(_ => { });
        public System.Action<List<BIMImportSaveData>, List<BIMImportSaveData>> projectChangeCallback = (_, _) => { };

        public BIMImportSaveLoadSystem(SaveSystem saveSystem)
        {
            saveSystem.SaveEvent += Save;
            saveSystem.LoadEvent += Load;
            saveSystem.DeleteEvent += OnDelete;
            saveSystem.ProjectChangedEvent += OnProjectChanged;
        }

        public bool AddSaveData(BIMImportSaveData data)
        {
            if (saveDatas.Any(x => x.ID == data.ID))
            {
                return false;
            }
            saveDatas.Add(data);
            ProjectSaveDataManager.Add(
                ProjectSaveDataType.BimImport,
                data.ID);
            return true;
        }

        public void UpdateSaveData(Vector3 position, Vector3 angle, Vector3 scale, string id)
        {
            var data = saveDatas.FirstOrDefault(x => x.ID == id);
            data?.SetTransform(position, angle, scale);
        }

        public bool RemoveSaveData(string name)
        {
            var data = saveDatas.FirstOrDefault(x => x.ID == name);
            if (data == null)
            {
                return false;
            }
            saveDatas.Remove(data);
            
            ProjectSaveDataManager.Delete(
                ProjectSaveDataType.BimImport,
                data.ID);
            return true;
        }

        private void Save(string projectID)
        {
            List<BIMImportSaveData> filteredData;
            if (string.IsNullOrEmpty(projectID))
            {
                filteredData = saveDatas;
            }
            else
            {
                filteredData = saveDatas
                    .Where(data => ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.BimImport,
                        projectID,
                        data.ID))
                    .ToList();
            }

            DataSerializer.Save("BIM", filteredData);
        }

        /// <summary>
        /// Load -> BIMIMportSaveLoadSystem.SaveDataListの読み出しの順で呼び出す
        /// </summary>
        private void Load(string projectID)
        {
            var datas = DataSerializer.Load<List<BIMImportSaveData>>("BIM");
            if (datas == null || datas.Count == 0)
            {
                return;
            }
            loadCallback?.Invoke(datas);
        }
        
        private void OnDelete(string projectID)
        {
            var deleteList = saveDatas.Where(data => ProjectSaveDataManager.TryCheckData(
                    ProjectSaveDataType.BimImport,
                    projectID,
                    data.ID,
                    false))
                .ToList();
            
            foreach (var bimImportSaveData in deleteList)
            {
                saveDatas.Remove(bimImportSaveData);
            }
            deleteCallback?.Invoke(deleteList);
        }
        
        private void OnProjectChanged(string projectID)
        {
            var canEditList = saveDatas.Where(data => ProjectSaveDataManager.TryCheckData(
                    ProjectSaveDataType.BimImport,
                    projectID,
                    data.ID))
                .ToList();
            var notEditList = saveDatas.Except(canEditList).ToList();
            
            projectChangeCallback?.Invoke(canEditList, notEditList);
        }
    }
}
