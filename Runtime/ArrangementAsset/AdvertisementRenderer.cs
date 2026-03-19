using Landscape2.Runtime.Common;
using PlateauToolkit.Sandbox.Runtime;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 広告の描画機能
    /// </summary>
    public class AdvertisementRenderer
    {
        /// <summary>
        /// 画像動画周り用の広告アセットインターフェース
        /// PlateauSandboxAdvertisement と PlateauSandboxAdvertisementScaled の両方に対応するため追加
        /// </summary>
        private interface IAdAsset
        {
            int[] targetMaterialNumbers { get; }
            string targetTextureProperty { get; }
            PlateauSandboxAdvertisement.AdvertisementType advertisementType { get; set; }
            List<PlateauSandboxAdvertisement.AdvertisementMaterials> advertisementMaterials { get; }
            Texture advertisementTexture { get; set; }
            public VideoPlayer VideoPlayer { get; }
            string name { get; }

            void AddVideoPlayer();
            void SetTexture();
            void SetMaterials();
            void SetupAdvertisementMaterials();
        }

        /// <summary>
        /// PlateauSandboxAdvertisement 用のアダプター
        /// </summary>
        private class AdAdapter : IAdAsset
        {
            private PlateauSandboxAdvertisement ad;
            private readonly int[] _targetMaterialNumbers = new int[] { };

            public int[] targetMaterialNumbers => _targetMaterialNumbers;

            public AdAdapter(PlateauSandboxAdvertisement ad)
            {
                this.ad = ad;
                if (ad.name == "Advertisement_WallSignboard")
                {
                    // WallSignboardだけ例外
                    _targetMaterialNumbers = new int[] { 1 };
                }
                else
                {
                    _targetMaterialNumbers = new int[] { ad.targetMaterialNumber };
                }
            }

            public int targetMaterialNumber => ad.targetMaterialNumber;
            public string targetTextureProperty => ad.targetTextureProperty;
            public PlateauSandboxAdvertisement.AdvertisementType advertisementType
            {
                get => ad.advertisementType;
                set => ad.advertisementType = value;
            }
            public List<PlateauSandboxAdvertisement.AdvertisementMaterials> advertisementMaterials => ad.advertisementMaterials;
            public Texture advertisementTexture
            {
                get => ad.advertisementTexture;
                set => ad.advertisementTexture = value;
            }
            public VideoPlayer VideoPlayer => ad.VideoPlayer;
            public string name => ad.name;

            public void AddVideoPlayer() => ad.AddVideoPlayer();
            public void SetTexture() => ad.SetTexture();
            public void SetMaterials() => ad.SetMaterials();
            public void SetupAdvertisementMaterials() { }
        }

        /// <summary>
        /// PlateauSandboxAdvertisementScaled 用のアダプター
        /// </summary>
        private class AdScaledAdapter : IAdAsset
        {
            private PlateauSandboxAdvertisementScaled ad;
            private readonly int[] _targetMaterialNumbers = new int[] { };

            public int[] targetMaterialNumbers => _targetMaterialNumbers;

            public AdScaledAdapter(PlateauSandboxAdvertisementScaled ad)
            {
                this.ad = ad;
                string[] table = new[] { "Advertisement_Billboard", "Advertisement_Billboard_Single" };
                if (table.Contains(ad.name))
                {
                    // Billboardは2つ広告面のマテリアルがある
                    _targetMaterialNumbers = new int[] { 1, 2 };
                }
                else
                {
                    _targetMaterialNumbers = new int[] { 1 };
                }
            }

            public int targetMaterialNumber => ad.targetMaterialNumber;
            public string targetTextureProperty => ad.targetTextureProperty;
            public PlateauSandboxAdvertisement.AdvertisementType advertisementType
            {
                get => ad.advertisementType;
                set => ad.advertisementType = value;
            }
            public List<PlateauSandboxAdvertisement.AdvertisementMaterials> advertisementMaterials => ad.advertisementMaterials;
            public Texture advertisementTexture
            {
                get => ad.advertisementTexture;
                set => ad.advertisementTexture = value;
            }
            public VideoPlayer VideoPlayer => ad.VideoPlayer;
            public string name => ad.name;

            public void AddVideoPlayer() => ad.AddVideoPlayer();
            public void SetTexture() => ad.SetTexture();
            public void SetMaterials() => ad.SetMaterials();
            public void SetupAdvertisementMaterials()
            {
                if (advertisementMaterials != null && advertisementMaterials.Count > 0) return;
                ad.Reset();
                ad.targetMaterialNumber = 1;
            }
        }

        private readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg" };
        private readonly string[] videoExtensions = { ".mov", ".mp4" };

        // private PlateauSandboxAdvertisement target;
        private IAdAsset target;

        public string SelectFile(bool isImage)
        {
            // 拡張子で指定してファイル選択
            var fileExtensions = isImage
                 ? new ExtensionFilter("Image Files",
                     imageExtensions.Select(ext => ext.Split('.')[1]).ToArray())
                 : new ExtensionFilter("Video Files",
                     videoExtensions.Select(ext => ext.Split('.')[1]).ToArray());

            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select File", "", new[] { fileExtensions }, false);
            if (paths.Length == 0)
            {
                return string.Empty;
            }
            return paths[0];
        }

        public void Render(GameObject selectObject, string filePath)
        {
            if (selectObject.TryGetComponent<PlateauSandboxAdvertisement>(out var outTarget))
            {
                target = new AdAdapter(outTarget);
            }
            else if (selectObject.TryGetComponent<PlateauSandboxAdvertisementScaled>(out var outTargetScaled))
            {
                target = new AdScaledAdapter(outTargetScaled);
                target.SetupAdvertisementMaterials();
            }

            if (target == null || string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // Videoを止める
            if (target.VideoPlayer != null)
            {
                target.VideoPlayer.Stop();
            }

            // 拡張子から描画処理を分岐
            string fileExtension = Path.GetExtension(filePath).ToLower();
            if (imageExtensions.Contains(fileExtension))
            {
                var texture = FileUtil.LoadTexture(filePath);
                RenderTexture(texture);
            }
            else if (videoExtensions.Contains(fileExtension))
            {
                PrepareVideo(filePath);
            }
            else
            {
                Debug.LogWarning("サポートされている拡張子ではありません");
            }

            // プロジェクト保存用にPersistentDataにコピー
            FileUtil.CopyToPersistentData(filePath, target.name);
        }

        private void RenderTexture(Texture texture)
        {
            if (texture == null)
            {
                Debug.LogWarning("テクスチャが読み込めませんでした");
                return;
            }

            target.SetTexture();
            target.advertisementTexture = texture;
            target.advertisementType = PlateauSandboxAdvertisement.AdvertisementType.Image;

            for (int i = 0; i < target.targetMaterialNumbers.Length; i++)
            {
                var targetMaterialNumber = target.targetMaterialNumbers[i];
                // マテリアル複製
                Material mat = target.advertisementMaterials[0].materials[targetMaterialNumber];
                var duplicatedMat = new Material(mat)
                {
                    name = $"{mat.name}_Instance_for_{target.name}"
                };
                // duplicatedMat.SetTexture(target.targetTextureProperty, target.advertisementTexture);
                duplicatedMat.mainTexture = target.advertisementTexture;

                // マテリアルを差し替え
                foreach (PlateauSandboxAdvertisement.AdvertisementMaterials advertisementMaterial in target.advertisementMaterials)
                {
                    if (advertisementMaterial.materials.Count > targetMaterialNumber)
                    {
                        advertisementMaterial.materials[targetMaterialNumber] = duplicatedMat;
                    }
                }
            }
            target.SetMaterials();
        }

        private void PrepareVideo(string filePath)
        {
            target.AddVideoPlayer();
            target.advertisementType = PlateauSandboxAdvertisement.AdvertisementType.Video;
            target.VideoPlayer.url = filePath;

            // 重複を防ぐために削除
            target.VideoPlayer.prepareCompleted -= RenderVideo;
            target.VideoPlayer.errorReceived -= OnFailedVideo;

            // コールバック登録
            target.VideoPlayer.prepareCompleted += RenderVideo;
            target.VideoPlayer.errorReceived += OnFailedVideo;

            // 動画の準備
            target.VideoPlayer.Prepare();
        }

        private void RenderVideo(VideoPlayer videoPlayer)
        {
            var duplicatedMaterials = new List<Material>();
            for (int i = 0; i < target.targetMaterialNumbers.Length; i++)
            {
                var targetMaterialNumber = target.targetMaterialNumbers[i];
                // マテリアル複製
                Material mat = target.advertisementMaterials[0].materials[targetMaterialNumber];
                var duplicatedMat = new Material(mat)
                {
                    name = $"{mat.name}_Instance_for_{target.name}"
                };

                // マテリアルを差し替え
                foreach (PlateauSandboxAdvertisement.AdvertisementMaterials advertisementMaterial in target.advertisementMaterials)
                {
                    if (advertisementMaterial.materials.Count > targetMaterialNumber)
                    {
                        advertisementMaterial.materials[targetMaterialNumber] = duplicatedMat;
                    }
                }
                duplicatedMaterials.Add(duplicatedMat);
            }
            target.SetMaterials();

            // RenderTexture作成
            var renderTexture = new RenderTexture(
                (int)videoPlayer.width <= 0 ? 1 : (int)videoPlayer.width,
                (int)videoPlayer.height <= 0 ? 1 : (int)videoPlayer.height,
                0);
            renderTexture.Create();
            videoPlayer.targetTexture = renderTexture;
            foreach (var duplicatedMat in duplicatedMaterials)
            {
                // duplicatedMat.SetTexture(target.targetTextureProperty, renderTexture);
                // duplicatedMat.SetTexture("_BaseMapColor", renderTexture);
                // duplicatedMat.SetTexture("_MainTex", renderTexture);
                duplicatedMat.mainTexture = renderTexture;
            }

            // 動画再生
            videoPlayer.Play();
        }

        private void OnFailedVideo(VideoPlayer videoPlayer, string message)
        {
            Debug.LogWarning($"{videoPlayer.name} の再生時にエラーが発生しました: {message}");
        }
    }
}