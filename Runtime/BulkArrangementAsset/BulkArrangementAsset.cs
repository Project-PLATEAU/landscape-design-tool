using PlateauToolkit.Sandbox.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace Landscape2.Runtime
{
    public class BulkArrangementAsset
    {
        private PlateauSandboxBulkPlaceDataContext dataContext = new();
        
        private bool isIgnoreHeight;
        public bool IsIgnoreHeight => isIgnoreHeight;
        
        private int selectedID = -1;
        public int SelectedID => selectedID;
        
        // 更新イベント
        public UnityEvent<int> OnUpdatedSelectAsset = new();
        public UnityEvent OnUpdatedAssetTypes = new();
        
        private List<PlateauSandboxBulkPlaceHierarchyItem> assetTypes = new();
        public List<PlateauSandboxBulkPlaceHierarchyItem> AssetTypes => assetTypes;
        
        public bool TryLoadFile(string filePath)
        {
            return dataContext.TryFileParse(filePath);
        }
        
        public bool IsLoadedCsv()
        {
            return dataContext.GetFileType() == PlateauSandboxBulkPlaceFileType.k_Csv;
        }
        
        public string[] GetFieldLabels()
        {
            return dataContext.GetFieldLabels();
        }
        
        public int GetFieldIndex(PlateauSandboxBulkPlaceCategory category)
        {
            return dataContext.GetFieldIndex(category);
        }
        
        public void ReplaceField(int oldIndex, int newIndex)
        {
            dataContext.ReplaceField(oldIndex, newIndex);
        }
        
        public void SetIgnoreHeight(bool isIgnore)
        {
            isIgnoreHeight = isIgnore;
        }
        
        public void SetSelectedID(int id)
        {
            selectedID = id;
        }

        public void LoadAssetTypes()
        {
            assetTypes = dataContext.Data
                .GroupBy(data => data.AssetType)
                .Select(group => (group.Key, group.Count()))
                .Select((group, index) => (group, index))
                .ToList()
                .Select(asset =>
                {
                    string categoryName = asset.group.Key;
                    int count = asset.group.Item2;
                    return new PlateauSandboxBulkPlaceHierarchyItem()
                    {
                        ID = asset.index,
                        CategoryName = categoryName,
                        Count = count,
                    };
                }).ToList();
            
            OnUpdatedAssetTypes.Invoke();
        }
        
        public void SetPrefabId(int prefabId)
        {
            if (selectedID < 0)
            {
                return;
            }

            var item = assetTypes.FirstOrDefault(i => i.ID == selectedID);
            if (item != null)
            {
                item.PrefabConstantID = prefabId;
                
                // アセット更新
                OnUpdatedSelectAsset.Invoke(prefabId);
            }
        }
        
        public List<PlateauSandboxBulkPlaceDataBase> GetParsedData()
        {
            return dataContext.Data;
        }

        public void Reset()
        {
            dataContext.Clear();
            selectedID = -1;
            isIgnoreHeight = false;
            assetTypes.Clear();
        }
    }
}