using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ToolBox.Serialization;
using System.Linq;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;  // Taskを使用するために必要

namespace Landscape2.Runtime
{
    public class AssetsSubscribeSaveSystem
    {
        private SaveSystem saveSystem;
        private SaveLoadHandler saveLoadHandler = new();
        public SaveLoadHandler SaveLoadHandler => saveLoadHandler;

        public AssetsSubscribeSaveSystem(SaveSystem saveSystemInstance)
        {
            saveSystem = saveSystemInstance;
            GetPlateauAssetList();       
        }
        private async void GetPlateauAssetList()
        {
            AsyncOperationHandle<IList<GameObject>> plateauAssetHandle = Addressables.LoadAssetsAsync<GameObject>("Plateau_Assets", null);
            IList<GameObject> plateauAssetsList = await plateauAssetHandle.Task;
            saveLoadHandler.AddAsset(plateauAssetsList);
            SetEvents();
        }

        private void SetEvents()
        {
            saveSystem.SaveEvent += saveLoadHandler.SaveInfo;
            saveSystem.LoadEvent += saveLoadHandler.LoadInfo;
            saveSystem.DeleteEvent += saveLoadHandler.DeleteInfo;
            saveSystem.ProjectChangedEvent += saveLoadHandler.SetProjectInfo;
        }
    }
}
