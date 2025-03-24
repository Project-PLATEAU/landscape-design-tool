using Landscape2.Runtime.SaveData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ToolBox.Serialization;
using System.Linq;
using UnityEngine.Events;

namespace Landscape2.Runtime
{
    public class SaveLoadHandler
    {
        private IList<GameObject> plateauAssets;
        
        public UnityEvent<List<GameObject>> OnDeleteAssets = new();
        public UnityEvent<List<GameObject>, List<GameObject>> OnChangeEditableState = new();
        
        public void AddAsset(IList<GameObject> assets)
        {
            plateauAssets = assets;
        }

        public void SaveInfo(string projectID)
        {
            List<ArrangementSaveData> saveAssets = new List<ArrangementSaveData>();
            GameObject createdAssets = GameObject.Find("CreatedAssets");
            foreach(Transform asset in createdAssets.transform)
            {
                var saveData = new ArrangementSaveData();
                saveData.Save(asset);

                if (!string.IsNullOrEmpty(projectID))
                {
                    // プロジェクトIDが指定されている場合は、プロジェクトIDのデータのみ保存
                    if (!ProjectSaveDataManager.TryCheckData(
                            ProjectSaveDataType.Asset,
                            projectID,
                            asset.gameObject.GetInstanceID().ToString()))
                    {
                        continue;
                    }
                }
                
                saveAssets.Add(saveData);
            }
            DataSerializer.Save("Assets", saveAssets);
        }
        
        public void LoadInfo(string projectID)
        {
            List<ArrangementSaveData> loadedTransformData = DataSerializer.Load<List<ArrangementSaveData>>("Assets");
            if (loadedTransformData != null)
            {
                GameObject createdAssets = GameObject.Find("CreatedAssets");
                
                var assets = new List<Transform>();
                
                // ロードしたアセットの生成
                foreach(ArrangementSaveData savedData in loadedTransformData)
                {
                    var transformData = savedData.transformData;
                    string assetName = transformData.name;
                    GameObject asset = plateauAssets.FirstOrDefault(p => p.name == assetName);
                    GameObject generatedAsset = GameObject.Instantiate(asset, transformData.position, transformData.rotation, createdAssets.transform) as GameObject;
                    
                    // 生成したオブジェクトにデータを反映
                    savedData.Apply(generatedAsset);
                    
                    // リストに追加
                    ArrangementAssetListUI.OnCreatedAsset.Invoke(generatedAsset);
                    
                    // プロジェクトに通知
                    ProjectSaveDataManager.Add(ProjectSaveDataType.Asset, generatedAsset.gameObject.GetInstanceID().ToString(), projectID, false);
                    
                    assets.Add(generatedAsset.transform);
                }
                
                //表示を切り替え
                SetProjectInfo(projectID);
            }
            else
            {
                Debug.LogError("No saved project data found.");
            }
        }

        public void DeleteInfo(string projectID)
        {
            GameObject createdAssets = GameObject.Find("CreatedAssets");
            
            var deleteAssets = new List<GameObject>();
            foreach(Transform asset in createdAssets.transform)
            {
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.Asset,
                        projectID,
                        asset.gameObject.GetInstanceID().ToString(),
                        false))
                {
                    deleteAssets.Add(asset.gameObject);
                }
            }
            OnDeleteAssets.Invoke(deleteAssets);
        }
        
        public void SetProjectInfo(string projectID)
        {
            GameObject createdAssets = GameObject.Find("CreatedAssets");
            var editableAssets = new List<GameObject>();
            var notEditableAssets = new List<GameObject>();
            
            foreach(Transform asset in createdAssets.transform)
            {
                if (ProjectSaveDataManager.TryCheckData(
                        ProjectSaveDataType.Asset,
                        projectID,
                        asset.gameObject.GetInstanceID().ToString()))
                {
                    editableAssets.Add(asset.gameObject);
                }
                else
                {
                    notEditableAssets.Add(asset.gameObject);
                }
            }
            OnChangeEditableState.Invoke(editableAssets, notEditableAssets);
        }
    }
}
