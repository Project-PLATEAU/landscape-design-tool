using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Landscape2.Runtime
{
    public class BIMImportBuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;

        private readonly string ifcExeName = "IfcConvert";
        private readonly string ifcPath = Path.Combine(Application.dataPath, "IfcConvert");

        /// <summary>
        /// build前にifcconverterの有無をvalidationにかける
        /// </summary>
        /// <param name="report"></param>
        /// <exception cref="BuildFailedException"></exception>
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"{nameof(BIMImportBuildHook)}.OnPreprocessBuild");

            // フォルダが無いとダメ!
            if (!Directory.Exists(ifcPath))
            {
                Directory.CreateDirectory(ifcPath);
                Debug.Log($"IfcConverterフォルダを作成しました: {ifcPath}");
            }

            Debug.Log($"pass ifcPath : {ifcPath}");

            // 実行ファイルが無いのもダメ!
            var ifcExecWithoutExt = ifcExeName;
            var ifcExec = ifcExeName;
#if UNITY_STANDALONE_WIN
            ifcExec = $"{ifcExec}.exe";
#endif
            var exeFullPath = Path.Combine(ifcPath, ifcExec);
            // Resources.Loadで.bytesファイルを確認
            var execData = Resources.Load<TextAsset>("IfcConvert");

            // 通常のパスとResources配下の両方を確認
            if (!File.Exists(exeFullPath) && execData == null)
            {
                string errMes = $"{ifcExec} がありません";
                EditorUtility.DisplayDialog("Error in BIMLoader", errMes, "cancel");
                throw new BuildFailedException(errMes);
            }

            // Resources配下に存在する場合は、そちらをコピー元として使用
            string dstPath = Path.Combine(Application.streamingAssetsPath, ifcExec);
            if (File.Exists(dstPath))
            {
                // 既にあるのでcopyをしない
                return;
            }

            // StreamingAssetsフォルダが無ければ作成
            Directory.CreateDirectory(Application.streamingAssetsPath);

            if (execData != null)
            {
                // Resourcesから読み込んだデータを直接書き出し
                File.WriteAllBytes(dstPath, execData.bytes);
                Debug.Log($"Copied from Resources to: {dstPath}");
            }
            else
            {
                // 通常パスからコピー
                File.Copy(exeFullPath, dstPath);
                Debug.Log($"Copied from: {exeFullPath} to: {dstPath}");
            }
        }
    }
}
