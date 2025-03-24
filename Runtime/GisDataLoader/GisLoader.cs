using Landscape2.Runtime.UiCommon;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GISデータのロードを管理するクラス
    /// </summary>
    public class GisLoader
    {
        // ロード済みのGISデータリスト
        private List<GisData> gisDataList = new();
        public List<GisData> GisDataList => gisDataList;
        
        // GISデータのロードを通知
        public UnityEvent OnLoad = new();
        
        // GISデータのクリアを通知
        public UnityEvent OnClearAll = new();

        // ShapeFileローダー
        private readonly ShapefileLoader shapefileLoader = new();
        
        // GeoJsonローダー
        private readonly GeoJsonLoader geoJsonLoader = new();

        public void Load(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            var folderPath = Path.GetDirectoryName(filePath);
            if (extension == ".shp")
            {
                gisDataList = shapefileLoader.Load(folderPath);
            }
            else if (extension == ".geojson")
            {
                gisDataList = geoJsonLoader.Load(folderPath);
            }
            else
            {
                ModalUI.ShowModal("GISデータ読み込み", "シェープファイルまたはGeoJsonが見つかりませんでした。", false, true);
                return;
            }

            if (gisDataList.Count > 0)
            {
                // ロード完了を通知
                OnLoad.Invoke();
            }
        }

        public void Clear()
        {
            gisDataList.Clear();
            OnClearAll.Invoke();
        }

        public List<string> GetAttributeList()
        {
            // 最初のデータを参考としてリストに表示する
            var sampleData = gisDataList.FirstOrDefault();
            if (sampleData == null)
            {
                return new List<string>();
            }
            
            return sampleData.Attributes
                .Select(data => $"{data.Key}({data.Value})")
                .ToList();
        }

        public List<(string key, string value)> GetAttributes(int attributeIndex)
        {
            // 特定の属性のリストを取得
            return gisDataList
                .Select(data => data.Attributes.ElementAt(attributeIndex))
                .Select(a => (a.Key, a.Value))
                .ToList();
        }
    }
}