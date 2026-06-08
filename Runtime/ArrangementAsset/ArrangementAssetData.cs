using PlateauToolkit.Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Landscape2.Runtime
{
    /// <summary>
    /// ArrangementAssetからaddressableのデータ読み込み処理を抽出した
    /// </summary>
    public class ArrangementAssetData
    {
        protected virtual async Task<Tuple<IList<GameObject>, IList<Texture2D>>> LoadPlateauAssets(string keyName)
        {
            AsyncOperationHandle<IList<GameObject>> plateauAssetHandle = Addressables.LoadAssetsAsync<GameObject>(keyName, null);
            IList<GameObject> assetsList = await plateauAssetHandle.Task;
            assetsList = assetsList.Where(n => n.GetComponent<PlateauSandboxPlaceableHandler>() != null).ToList();

            AsyncOperationHandle<IList<Texture2D>> assetsPictureHandle = Addressables.LoadAssetsAsync<Texture2D>("AssetsPicture", null);
            IList<Texture2D> assetsPicture = await assetsPictureHandle.Task;

            // arrangementAssetUIClass.RegisterCategoryPanelAction(buttonName, assetsList, assetsPicture);
            return new Tuple<IList<GameObject>, IList<Texture2D>>(assetsList, assetsPicture);
        }
    }
}
