using PlateauToolkit.Sandbox.Runtime;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Landscape2.Runtime.Common;
using FileUtil = Landscape2.Runtime.Common.FileUtil;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 広告データの保存
    /// </summary>
    [Serializable]
    public class AdvertisementSaveData : IArrangementSandboxAssetSaveData<PlateauSandboxAdvertisement>
    {
        public PlateauSandboxAdvertisement.AdvertisementType advertisementType;
        public string texturePath;
        public string videoPath;

        public void Save(PlateauSandboxAdvertisement advertisement)
        {
            // ファイルパスを保存
            var path = FileUtil.GetPersistentPath(advertisement.name);
            
            advertisementType = advertisement.advertisementType;
            switch (advertisementType)
            {
                case PlateauSandboxAdvertisement.AdvertisementType.Image:
                    texturePath = path;
                    break;
                case PlateauSandboxAdvertisement.AdvertisementType.Video:
                    videoPath = path;
                    break;
            }
        }
        
        public void Apply(PlateauSandboxAdvertisement target)
        {
            var filePath = advertisementType == PlateauSandboxAdvertisement.AdvertisementType.Image ?
                texturePath : videoPath;
            var renderer = new AdvertisementRenderer();
            renderer.Render(target.gameObject, filePath);
        }
    }
}