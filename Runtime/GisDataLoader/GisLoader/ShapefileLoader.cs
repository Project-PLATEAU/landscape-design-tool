using CesiumForUnity;
using Landscape2.Runtime.LandscapePlanLoader;
using PlateauToolkit.Maps;
using PlateauToolkit.Sandbox.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TriangleNet.Geometry;
using Unity.Mathematics;
using UnityEngine;
using IShape = PlateauToolkit.Maps.IShape;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// Shapefileのローダー
    /// </summary>
    public class ShapefileLoader : GisLoaderBase
    {
        // サポートするエンコーディング
        private SupportedEncoding supportedStringEncoding = SupportedEncoding.ShiftJIS;
        
        private List<IShape> listOfShapes = new List<IShape>();
        private int shapeType;
     
        private List<GisData> loadedGisData = new List<GisData>();
        
        public ShapefileLoader() : base()
        {
        }

        public override List<GisData> Load(string folderPath)
        {
            // ロード済みのGISデータリストをクリア
            loadedGisData.Clear();
            
            if (!IsValidInLoad(folderPath))
            {
                return loadedGisData;
            }

            try
            {
                ReadShape(folderPath);
                ReadDbf(folderPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{folderPath} のシェープファイルの読み込みに失敗しました。 {e.Message}");
                return loadedGisData;
            }

            return loadedGisData;
        }

        private void ReadShape(string folderPath)
        {
            string[] filePaths = Directory.GetFiles(folderPath, "*.shp");
            if (filePaths.Length == 0)
            {
                Debug.LogWarning($"{folderPath} にシェープファイルが見つかりませんでした。");
                return;
            }
            
            using (ShapefileReader reader = new ShapefileReader(filePaths[0]))
            {
                listOfShapes = reader.ReadShapes();
                shapeType = reader.ShapeConstants;
            }
        }

        private void ReadDbf(string folderPath)
        {
            string[] filePaths = Directory.GetFiles(folderPath, "*.dbf");
            if (filePaths.Length == 0)
            {
                Debug.LogWarning($"{folderPath} にDBFファイルが見つかりませんでした。");
                return;
            }
            
            using (DbfReader reader = new DbfReader(filePaths[0], supportedStringEncoding))
            {
                reader.ReadHeader();
                
                // ロードを実行
                LoadGisData(reader);
            }
        }

        private void LoadGisData(DbfReader dbfReader)
        {
            // 高さは0でセット
            var height = 0f;
            
            foreach (IShape shape in listOfShapes)
            {
                var gisData = new GisData();
                var record = dbfReader.ReadNextRecord();
                switch (shapeType)
                {
                    case 3:
                    case 5:
                        for (int i = 0; i < shape.Parts.Count - 1; i++)
                        {
                            int start = shape.Parts[i];
                            int end = shape.Parts[i + 1];

                            // get the points
                            var partPoints = shape.Points.GetRange(start, end - start);
                            
                            foreach (Vector3 point in partPoints)
                            {
                                double3 coordinates = new(point.x, point.z, height);
                                gisData.WorldPoints.Add(ConvertToWorldSpace(coordinates));
                            }
                        }
                        break;
                    
                    // ポイント
                    case 1:
                        foreach (Vector3 point in shape.Points)
                        {
                            double3 coordinates = new(point.x, point.z, height);
                            gisData.WorldPoints.Add(ConvertToWorldSpace(coordinates));
                        }
                        break;
                }
                
                gisData.Attributes = gisData.Attributes.Concat(GetAttribute(record)).ToList();
                
                // GISデータをセット
                loadedGisData.Add(gisData);
            }
        }

        private Vector3 ConvertToWorldSpace(double3 coordinate)
        {
            CesiumGlobeAnchor cesiumGlobeAnchor = cesiumGlobeAnchorObject.GetComponent<CesiumGlobeAnchor>();
            if (cesiumGlobeAnchor != null)
            {
                cesiumGlobeAnchorObject.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinate;
            }
            return cesiumGlobeAnchorObject.transform.position;
        }
        
        private List<KeyValuePair<string, string>> GetAttribute(DbfRecord record)
        {
            var attributes = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < record.FieldNames.Length; i++)
            {
                var key = record.FieldNames[i].Trim();
                var value = record.Fields[i].Trim();
                attributes.Add(
                    new KeyValuePair<string, string>(key, value));
            }
            return attributes;
        }
    }
}