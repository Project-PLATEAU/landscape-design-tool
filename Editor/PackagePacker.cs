using PLATEAU.Network;
using UnityEditor;
using UnityEditor.PackageManager;

namespace LandscapeDesignTool.Editor
{
    /// <summary>
    /// Packagesフォルダに入っている 景観まちづくりツール を tarball 形式で出力します。
    /// デプロイで利用します。
    /// </summary>
    public static class PackagePacker
    {
        [MenuItem("PLATEAU/景観まちづくり/開発者向け/Packageをtarballに出力")]
        public static void Pack()
        {
            var destDir = EditorUtility.SaveFolderPanel("出力先", "", "");
            if (string.IsNullOrEmpty(destDir)) return;
            UnityEditor.PackageManager.Client.Pack("Packages/com.synesthesias.landscape-design-tool", destDir);
        }
    }
}
