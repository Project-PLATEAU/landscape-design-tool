using CesiumForUnity;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISデータをロード規定クラス
    /// </summary>
    public abstract class GisLoaderBase
    {
        // ジオリファレンス、座標変換用に保持
        protected CesiumGeoreference geoRef;

        // 位置取得用のオブジェクト
        protected GameObject cesiumGlobeAnchorObject;

        protected GisLoaderBase()
        {
            geoRef = UnityEngine.Object.FindObjectOfType<CesiumGeoreference>();

            var anchorObject = UnityEngine.Object.FindObjectOfType<CesiumGlobeAnchor>();
            if (anchorObject == null)
            {
                cesiumGlobeAnchorObject = new GameObject("CesiumGlobeAnchor");
                cesiumGlobeAnchorObject.transform.SetParent(geoRef.transform);
                cesiumGlobeAnchorObject.AddComponent<CesiumGlobeAnchor>();
            }
            else
            {
                cesiumGlobeAnchorObject = anchorObject.gameObject;
            }
        }

        /// <summary>
        ///  フォルダからデータをロード
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public abstract List<GisData> Load(string folderPath);

        /// <summary>
        /// ロード時の共通バリデーションチェック
        /// </summary>
        /// <returns></returns>
        protected bool IsValidInLoad(string folderPath)
        {
            if (geoRef == null)
            {
                Debug.LogWarning("CesiumGeoreferenceが見つかりませんでした。");
                return false;
            }
            
            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"該当のフォルダが見つかりませんでした。 {folderPath}");
                return false;
            }
            
            return true;
        }
    }
}