using PLATEAU.Util;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace PLATEAU.Editor.DebugPlateau
{
    /// <summary>
    /// Packagesフォルダに入っているLandscape Design Tool を tarball 形式で出力します。
    /// デプロイで利用します。
    /// </summary>
    static class PackagePacker
    {
        private const string PACKAGE_NAME = "com.synesthesias.landscape-design-tool";
        [MenuItem("PLATEAU/Debug/Pack Landscape Package to tarball")]
        public static void PackLandScape()
        {
            string destDir = EditorUtility.SaveFolderPanel("出力先", "", "");
            if (string.IsNullOrEmpty(destDir))
            {
                // ユーザーがキャンセルした場合
                return;
            }
            
            try
            {
                var request = Client.Pack($"Packages/{PACKAGE_NAME}", destDir);
                while (!request.IsCompleted)
                {
                    // リクエストが完了するまで待機
                    System.Threading.Thread.Sleep(100);
                }

                if (request.Status == StatusCode.Success)
                {
                    EditorUtility.DisplayDialog("成功", $"パッケージが正常に出力されました: {destDir}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("エラー", $"パッケージの出力に失敗しました。\nステータス: {request.Status}\n出力先: {destDir}", "OK");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"パッケージの出力中にエラーが発生しました。\n{e.Message}\n出力先: {destDir}", "OK");
            }
        }
    }
}
