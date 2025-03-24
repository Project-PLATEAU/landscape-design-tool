using GLTFast;
using GLTFast.Materials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;


namespace Landscape2.Runtime
{
    public class BIMLoader : IDisposable
    {
        public class CustomHDRPMaterialGenerator : HighDefinitionRPMaterialGenerator
        {
            protected override Material GenerateDefaultMaterial(bool pointsSupport = false)
            {
                return base.GenerateDefaultMaterial(pointsSupport);
            }
            public override Material GenerateMaterial(GLTFast.Schema.MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
            {
                try
                {
                    // ベースのHDRPマテリアル生成
                    var material = base.GenerateMaterial(gltfMaterial, gltf, pointsSupport);

                    // glTF の PBR 情報に基づいて HDRP 用にさらにカスタマイズ
                    if (gltfMaterial.PbrMetallicRoughness != null)
                    {
                        // Base Color Factor
                        if (gltfMaterial.PbrMetallicRoughness.baseColorFactor != null)
                        {
                            var color = gltfMaterial.PbrMetallicRoughness.baseColorFactor;
                            material.SetColor("_BaseColor", new Color(color[0], color[1], color[2], color[3]));
                        }

                        // Metallic and Roughness
                        material.SetFloat("_Metallic", gltfMaterial.PbrMetallicRoughness.metallicFactor);
                        material.SetFloat("_Smoothness", 1.0f - gltfMaterial.PbrMetallicRoughness.roughnessFactor);
                    }

                    // Emissive Factor
                    if (gltfMaterial.Emissive != null)
                    {
                        var emissive = gltfMaterial.Emissive;
                        material.SetColor("_EmissiveColor", new Color(emissive[0], emissive[1], emissive[2]));
                    }

                    // Normal Map
                    if (gltfMaterial.NormalTexture?.index >= 0)
                    {
                        var normalTexture = gltf.GetTexture(gltfMaterial.NormalTexture.index);
                        if (normalTexture != null)
                        {
                            material.SetTexture("_NormalMap", normalTexture);
                            material.EnableKeyword("_NORMALMAP");
                        }
                    }

                    // Occlusion Map
                    if (gltfMaterial.OcclusionTexture?.index >= 0)
                    {
                        var occlusionTexture = gltf.GetTexture(gltfMaterial.OcclusionTexture.index);
                        if (occlusionTexture != null)
                        {
                            material.SetTexture("_OcclusionMap", occlusionTexture);
                        }
                    }
                    return material;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.ToString());
                }
                return null;
            }
        }

        /// <summary>
        /// fullPath渡すとファイルの有るフォルダを作成する。
        /// disposeするとフォルダ消す
        /// </summary>
        private class DstPathData : IDisposable
        {
            string fileDir = string.Empty;
            string fileName = string.Empty;

            public string Dir => fileDir;
            public string FullPath => Path.Combine(Dir, fileName);

            public DstPathData(string path = null)
            {
                if (path == null)
                {
                    Debug.LogWarning($"path is Null");
                    return;
                }
                var dir = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);

                fileDir = dir;
                if (Directory.Exists(fileDir))
                {
                    Dispose();
                }

                Directory.CreateDirectory(fileDir);
            }


            public void Dispose()
            {

                if (string.IsNullOrEmpty(fileDir))
                {
                    Debug.LogWarning($"fileDir is Null or Empty");
                    return;
                }
                if (Directory.Exists(fileDir))
                {
                    Directory.Delete(fileDir, true);
                }
            }
        }

        static BIMLoader _instance;

        public static BIMLoader Instance
        {
            get
            {
                _instance = new();
                return _instance;
            }
        }

        List<GltfImport> loadGLTFList = new();

        private BIMLoader() { }

        public async Task<(GameObject, byte[])> LoadBIM(string ifcFilePath)
        {
            // 実行ファイルのpathを取る
            var exePath = GetIfcConverterPath();

            // convertを実行(Item1がglbのpath、Item2がxmlのpath)。
            var result = ConvertIfc2gltf(ifcFilePath, exePath);

            // gltfファイルを開いて読み込む
            var loadResult = await LoadGlbBinary(result.Item1.FullPath);
            var importer = loadResult.Item1;

            // meshを作成して
            var go = new GameObject
            {
                name = Path.GetFileNameWithoutExtension(ifcFilePath)
            };
            await importer.InstantiateMainSceneAsync(go.transform);
            loadGLTFList.Add(importer);

            result.Item2.Dispose();
            result.Item1.Dispose();

            return (go, loadResult.Item2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<GameObject> CreateBIM(byte[] data)
        {
            var importer = await CreateGlbBinary(data);
            // meshを作成して
            var go = new GameObject();
            await importer.InstantiateMainSceneAsync(go.transform);
            loadGLTFList.Add(importer);

            return go;
        }

        private async Task<(GltfImport, byte[])> LoadGlbBinary(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);
#if true
            Debug.Log($"{filePath} : {data.Length}");
            var result = await CreateGlbBinary(data, new System.Uri(filePath));
            return (result, data);
#else
            var gltf = new GltfImport(null, null, new CustomHDRPMaterialGenerator());
            bool success = await gltf.LoadGltfBinary(
                data,
                // The URI of the original data is important for resolving relative URIs within the glTF
                new System.Uri(filePath)
                );
            if (success)
            {
                return gltf;
            }
            return null;
#endif
        }

        private async Task<GltfImport> CreateGlbBinary(byte[] data, Uri uri = null)
        {
            var gltf = new GltfImport(null, null, new CustomHDRPMaterialGenerator());
            bool success = await gltf.LoadGltfBinary(
                data, uri
                );
            if (success)
            {
                return gltf;
            }
            return null;

        }

        /// <summary>
        /// ifcconvert(.exe)のfullpathを取得する
        /// UnityEditorではAssets/IfcConvert/以下を想定
        /// RuntimeではStreamingAssets/IfcConvert/以下を想定
        /// </summary>
        /// <returns></returns>
        private string GetIfcConverterPath()
        {
            string fullPath = Path.Combine(Application.dataPath, "IfcConvert");
            string ifcExecName = string.Empty;

#if UNITY_EDITOR_WIN
            ifcExecName = "IfcConvert.exe";
#elif UNITY_STANDALONE_WIN
            fullPath = Path.Combine(Application.streamingAssetsPath,"IfcConvert");
            ifcExecName = "IfcConvert.exe";
#else
            ifcExecName = "IfcConvert";
#endif
            var result = Path.Combine(fullPath, ifcExecName);

            if (!File.Exists(result))
            {
                Debug.LogWarning($"{result} : {File.Exists(result)}");
                throw new FileNotFoundException(result);
            }

            return result;
        }

        private (DstPathData, DstPathData) ConvertIfc2gltf(string ifcFilePath, string ifcExecPath)
        {
            var tmpPath = Path.GetTempPath();
            var glbFileDir = Path.Combine(tmpPath, "glb");
            //var xmlFileDir = Path.Combine(tmpPath, "xml");

            var glbFilePath = Path.Combine(glbFileDir, $"{Path.GetFileNameWithoutExtension(ifcFilePath)}.glb");
            // var xmlFilePath = Path.Combine(xmlFileDir, $"{Path.GetFileNameWithoutExtension(ifcFilePath)}.xml");

            var glbPathData = new DstPathData(glbFilePath);
            // var xmlPathData = new DstPathData(xmlFilePath);


            var glbProcessInfo = GenerateProcessInformation(ifcExecPath, ifcFilePath, glbFilePath);
            // var xmlProcessInfo = GenerateProcessInformation(ifcExecPath, ifcFilePath, xmlFilePath);

            StartConversion(glbProcessInfo);
            // StartConversion(xmlProcessInfo);

#if UNITY_EDITOR
            System.Diagnostics.Process.Start(glbPathData.Dir);
#endif

            return new(glbPathData, new DstPathData());
        }

        private void StartConversion(System.Diagnostics.ProcessStartInfo processInfo)
        {
            float st = Time.realtimeSinceStartup;
            using (System.Diagnostics.Process ifcProcess = System.Diagnostics.Process.Start(processInfo))
            {
                ifcProcess.WaitForExit();
            }
            var pt = Time.realtimeSinceStartup - st;
        }
        /// <summary>
        /// Helper function to generate process information
        /// </summary>
        /// <param name="convertExePath"></param>
        /// <param name="ifcFilePath">Path to the source ifc file</param>
        /// <param name="outputPath">Path for the output file</param>
        /// <returns></returns>
        System.Diagnostics.ProcessStartInfo GenerateProcessInformation(string convertExePath, string ifcFilePath, string outputPath)
        {
            System.Diagnostics.ProcessStartInfo ifcProcessInfo =
                new System.Diagnostics.ProcessStartInfo(convertExePath)
                {
                    CreateNoWindow = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };
            string arguments = $"-j 8 --use-element-guids \"{ifcFilePath}\" \"{outputPath}\""; // -j はjobnumber。core数+1が良いとドキュメントには記載してあった
            ifcProcessInfo.Arguments = arguments;
            return ifcProcessInfo;
        }

        public void Dispose()
        {
            foreach (var importer in loadGLTFList)
            {
                importer.Dispose();
            }
        }
    }
}
