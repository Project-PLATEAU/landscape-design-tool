using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;
using SFB;
using System.Runtime.InteropServices;
using System.IO;

namespace Landscape2.Runtime
{
    /// <summary>
    /// Screenキャプチャ機能
    /// </summary>
    public class ScreenCapture
    {

        static ScreenCapture _instance = null;

        public static ScreenCapture Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                }
                return _instance;
            }
        }



        string saveDirPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

        private ScreenCapture() { }


        public async void OnClickCaptureButton()
        {
            var pngImage = await CaptureCameraImageToPNG(Camera.main);
            if (pngImage.Length < 1)
            {
                Debug.LogWarning($"画像の取得に失敗しました");
                return;
            }


            var fullPath = StandaloneFileBrowser.SaveFilePanel("Save ScreenCapture", saveDirPath, "", "png");

            if (string.IsNullOrEmpty(fullPath))
            {
                Debug.LogWarning($"保存先がnull or emptyです: {fullPath}");
                return;
            }

            // 保存して
            File.WriteAllBytes(fullPath, pngImage);

            // 保存先のpathを覚えておく
            saveDirPath = Path.GetDirectoryName(fullPath);

        }

        /// <summary>
        /// カメラ画像を取得
        /// </summary>
        /// <param name="cam"></param>
        async Task<byte[]> CaptureCameraImageToPNG(Camera cam)
        {
            byte[] pngImage = new byte[0];
            var rt = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 24, RenderTextureFormat.ARGB32);

            var currentTarget = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            cam.targetTexture = currentTarget;

            var req = AsyncGPUReadback.Request(rt, 0, async request =>
            {
                if (request.hasError)
                {
                    Debug.LogWarning($"画面のキャプチャに失敗しました");
                    return;
                }

                var rawData = request.GetData<Color32>();
                var dataFormat = rt.graphicsFormat;

                // これ別threadで動いていないかな？
                var image = ImageConversion.EncodeNativeArrayToPNG(rawData, dataFormat, (uint)request.width, (uint)request.height);

                pngImage = image.ToArray();
            });

            do
            {
                await Task.Yield();
            } while (!req.done);


            RenderTexture.ReleaseTemporary(rt);

            return pngImage;
        }
    }
}
