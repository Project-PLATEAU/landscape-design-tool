using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Landscape2.Runtime
{
    public static class ArrangementAssetLoader
    {
        private static Dictionary<ArrangementAssetType, List<GameObject>> assetDictionary = new();
        public static Dictionary<ArrangementAssetType, List<GameObject>> AssetDictionary => assetDictionary;
        private static List<Texture2D> pictures = new();
        
        public static void LoadAssets()
        {
            LoadAsset(ArrangementAssetType.Human);
            LoadAsset(ArrangementAssetType.Vehicle);
            LoadAsset(ArrangementAssetType.Building);
            LoadAsset(ArrangementAssetType.Plant);
            LoadAsset(ArrangementAssetType.Advertisement);
            LoadAsset(ArrangementAssetType.StreetFurniture);
            LoadAsset(ArrangementAssetType.Sign);
            LoadAsset(ArrangementAssetType.Miscellaneous);

            LoadPicture();
        }

        private static async void LoadAsset(ArrangementAssetType assetType)
        {
            var plateauAssetHandle = Addressables.LoadAssetsAsync<GameObject>(assetType.GetKeyName(), null);
            var assetList = await plateauAssetHandle.Task;

            SetAssets(assetType, assetList.ToList());
        }

        private static void SetAssets(ArrangementAssetType assetType, List<GameObject> assetList)
        {
            switch (assetType)
            {
                case ArrangementAssetType.Human:
                    assetDictionary.Add(ArrangementAssetType.Human, assetList);
                    break;
                case ArrangementAssetType.Vehicle:
                    assetDictionary.Add(ArrangementAssetType.Vehicle, assetList);
                    break;
                case ArrangementAssetType.Building:
                    assetDictionary.Add(ArrangementAssetType.Building, assetList);
                    break;
                case ArrangementAssetType.Plant:
                    assetDictionary.Add(ArrangementAssetType.Plant, assetList);
                    break;
                case ArrangementAssetType.Advertisement:
                    assetDictionary.Add(ArrangementAssetType.Advertisement, assetList);
                    break;
                case ArrangementAssetType.StreetFurniture:
                    assetDictionary.Add(ArrangementAssetType.StreetFurniture, assetList);
                    break;
                case ArrangementAssetType.Sign:
                    assetDictionary.Add(ArrangementAssetType.Sign, assetList);
                    break;
                case ArrangementAssetType.Miscellaneous:
                    assetDictionary.Add(ArrangementAssetType.Miscellaneous, assetList);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null);
            }
        }

        private static async void LoadPicture()
        {
            var assetsPictureHandle = Addressables.LoadAssetsAsync<Texture2D>("AssetsPicture", null);
            var assetsPicture = await assetsPictureHandle.Task;
            pictures = assetsPicture.ToList();
        }

        public static Texture2D GetPicture(string objectName)
        {
            return pictures.FirstOrDefault(picture => picture.name == objectName);
        }
        
        public static Texture2D GetPicture(int prefabID)
        {
            var prefab = GetAsset(prefabID);
            return prefab != null ? GetPicture(prefab.name) : null;
        }

        public static GameObject GetAsset(int prefabID)
        {
            return AssetDictionary.Values
                .SelectMany(assetList => assetList)
                .FirstOrDefault(asset => asset.GetInstanceID() == prefabID);
        }
    }
}