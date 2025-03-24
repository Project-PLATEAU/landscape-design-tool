using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    public static class FileUtil
    {
        // プロジェクト保存時に取得するためにファイルの名前とパスを保持
        private static Dictionary<string, string> PersistentPaths { get; } = new();

        public static Texture2D LoadTexture(string path)
        {
            var tmpTexture = new Texture2D(2, 2);
            var bytes = File.ReadAllBytes(path);
            tmpTexture.LoadImage(bytes);
            tmpTexture.Apply();
            return tmpTexture;
        }
        
        public static void CopyToPersistentData(string path, string keyName)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"{path} が見つかりませんでした。");
                return;
            }

            var fileName = Path.GetFileName(path);
            var savedPath = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(savedPath))
            {
                // 保存領域にコピー
                File.Copy(path, savedPath);
            }
            else
            {
                Debug.LogWarning($"{savedPath} すでに同じ名前のファイルが存在します。");
            }
            
            // 保存したパスを保持
            PersistentPaths.TryAdd(keyName, savedPath);
        }

        public static string GetPersistentPath(string keyName)
        {
            if (!PersistentPaths.ContainsKey(keyName))
            {
                Debug.LogWarning($"{keyName} が見つかりませんでした。");
                return "";
            }
            return PersistentPaths[keyName];
        }
    }
}