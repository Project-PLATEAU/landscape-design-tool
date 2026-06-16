using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Landscape2.Editor
{
    [InitializeOnLoad]
    public class InitializeAddressDatas
    {
        static InitializeAddressDatas()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        }

        private static void OnSceneChanged(Scene current, Scene next)
        {
            if(current.name == next.name || string.IsNullOrEmpty(next.name)) return;

            // ビルド中なら無視
            if(BuildPipeline.isBuildingPlayer) return;

            // 次のシーン名を取得
            string nextSceneName = next.name;

            // Assets/SampleDatas/SceneDatasにシーン名のフォルダがあれば、そちらからデータを持ってくる
            if (Directory.Exists(Application.dataPath + $"/SampleDatas/SceneDatas/{nextSceneName}"))
            {
                string addressDatasPath = Application.dataPath + "/SampleDatas/Resources/AddressDatas";
                string addressCachesPath = Application.dataPath + "/SampleDatas/Resources/AddressCache";
                if(Directory.Exists(addressDatasPath))
                {
                    Directory.Delete(addressDatasPath, true);
                }
                if(Directory.Exists(addressCachesPath))
                {
                    Directory.Delete(addressCachesPath, true);
                }
                Directory.CreateDirectory(addressDatasPath);
                Directory.CreateDirectory(addressCachesPath);
                string sceneAddressDatasPath = Application.dataPath + $"/SampleDatas/SceneDatas/{nextSceneName}/AddressDatas";
                string sceneAddressCachesPath = Application.dataPath + $"/SampleDatas/SceneDatas/{nextSceneName}/AddressCache";
                if (Directory.Exists(sceneAddressDatasPath))
                {
                    foreach (var file in Directory.GetFiles(sceneAddressDatasPath))
                    {
                        File.Copy(file, Path.Combine(addressDatasPath, Path.GetFileName(file)));
                    }
                }
                if (Directory.Exists(sceneAddressCachesPath))
                {
                    foreach (var file in Directory.GetFiles(sceneAddressCachesPath))
                    {
                        File.Copy(file, Path.Combine(addressCachesPath, Path.GetFileName(file)));
                    }
                }
                if((Directory.Exists(sceneAddressCachesPath) && Directory.GetFiles(sceneAddressDatasPath).Length > 0) 
                    || (Directory.Exists(sceneAddressCachesPath) && Directory.GetFiles(sceneAddressCachesPath).Length > 0))
                {
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
