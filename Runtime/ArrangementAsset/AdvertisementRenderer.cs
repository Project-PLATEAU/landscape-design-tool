using Landscape2.Runtime.Common;
using PlateauToolkit.Sandbox.Runtime;
using SFB;
using System;
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
        private readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg" };
        private readonly string[] videoExtensions = { ".mov", ".mp4" };
        
        private PlateauSandboxAdvertisement target;

        public string SelectFile(bool isImage)
        {
            // 拡張子で指定してファイル選択
            var fileExtensions = isImage
                 ? new ExtensionFilter("Image Files",
                     imageExtensions.Select(ext => ext.Split('.')[1]).ToArray())
                 : new ExtensionFilter("Video Files",
                     videoExtensions.Select(ext => ext.Split('.')[1]).ToArray());
            
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select File", "", new []{ fileExtensions }, false);
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
                target = outTarget;
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
            
            // マテリアル複製
            Material mat = target.advertisementMaterials[0].materials[target.targetMaterialNumber];
            var duplicatedMat = new Material(mat);
            duplicatedMat.SetTexture(target.targetTextureProperty, target.advertisementTexture);

            // マテリアルを差し替え
            foreach (PlateauSandboxAdvertisement.AdvertisementMaterials advertisementMaterial in target.advertisementMaterials)
            {
                advertisementMaterial.materials[target.targetMaterialNumber] = duplicatedMat;
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
            Material mat = target.advertisementMaterials[0].materials[target.targetMaterialNumber];

            // RenderTexture作成
            var renderTexture = new RenderTexture(
                (int)videoPlayer.width <= 0 ? 1 : (int)videoPlayer.width,
                (int)videoPlayer.height <= 0 ? 1 : (int)videoPlayer.height,
                0);
            renderTexture.Create();
            videoPlayer.targetTexture = renderTexture;
            mat.SetTexture(target.targetTextureProperty, renderTexture);
            
            // 動画再生
            videoPlayer.Play();
        }

        private void OnFailedVideo(VideoPlayer videoPlayer, string message)
        {
            Debug.LogWarning($"{videoPlayer.name} の再生時にエラーが発生しました: {message}");
        }
    }
}