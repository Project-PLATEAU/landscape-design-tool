using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Landscape2.Runtime.Common
{
    public class SpriteUtil
    {
        public static async void LoadAsset(string name, Action<Sprite> callback)
        {
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(name);
            await handle.Task;
            callback(handle.Result);
        }
    }
}