using PlateauToolkit.Sandbox.Runtime;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Landscape2.Runtime.Common;
using FileUtil = Landscape2.Runtime.Common.FileUtil;
using Landscape2.Runtime.AdRegulation;

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
        public Vector3 adSize;

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

            adSize = advertisement.adSize;
        }

        public void Apply(PlateauSandboxAdvertisement target)
        {
            var filePath = advertisementType == PlateauSandboxAdvertisement.AdvertisementType.Image ?
                texturePath : videoPath;
            var renderer = new AdvertisementRenderer();
            renderer.Render(target.gameObject, filePath);

            target.adSize = adSize;
            target.transform.localScale
                = new Vector3(
                    adSize.x / target.defaultAdSize.x,
                    adSize.y / target.defaultAdSize.y,
                    adSize.z / target.defaultAdSize.z);
        }
    }

    /// <summary>
    /// Scaled広告データの保存
    /// </summary>
    [Serializable]
    public class AdvertisementScaledSaveData : IArrangementSandboxAssetSaveData<PlateauSandboxAdvertisementScaled>
    {
        public float width;
        public float height;
        public float depth;
        public float adHeight;
        public float adLength;
        public PlateauSandboxAdvertisement.AdvertisementType advertisementType;
        public string texturePath;
        public string videoPath;

        public void Save(PlateauSandboxAdvertisementScaled advertisement)
        {
            IAdSizeSettingModule adSizeSettingModule = new AdSizeSettingModule();
            adSizeSettingModule.SetScalableAd(advertisement);

            width = adSizeSettingModule.Width;
            height = adSizeSettingModule.Height;
            depth = adSizeSettingModule.Depth;
            adHeight = adSizeSettingModule.AdHeight;
            adLength = adSizeSettingModule.AdLength;

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

        public void Apply(PlateauSandboxAdvertisementScaled target)
        {
            IAdSizeSettingModule adSizeSettingModule = new AdSizeSettingModule();
            adSizeSettingModule.SetScalableAd(target);

            adSizeSettingModule.Width = width;
            adSizeSettingModule.Height = height;
            adSizeSettingModule.Depth = depth;
            adSizeSettingModule.AdHeight = adHeight;
            adSizeSettingModule.AdLength = adLength;

            var filePath = advertisementType == PlateauSandboxAdvertisement.AdvertisementType.Image ?
                texturePath : videoPath;
            var renderer = new AdvertisementRenderer();
            renderer.Render(target.gameObject, filePath);
        }
    }
}