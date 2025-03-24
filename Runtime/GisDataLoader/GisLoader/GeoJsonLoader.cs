using CesiumForUnity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Landscape2.Runtime.GisDataLoader
{
    /// <summary>
    /// GeoJsonのローダー
    /// </summary>
    public class GeoJsonLoader : GisLoaderBase
    {
        public GeoJsonLoader() : base()
        {
        }

        public override List<GisData> Load(string folderPath)
        {
            var result = new List<GisData>();
            
            if (!IsValidInLoad(folderPath))
            {
                return result;
            }
            
            string[] filePaths = Directory.GetFiles(folderPath, "*.geojson");
            if (filePaths.Length == 0)
            {
                Debug.LogWarning($"GeoJsonが見つかりませんでした。 {folderPath}");
                return result;
            }
            
            try
            {
                string jsonString = File.ReadAllText(filePaths[0]);
                var geoJson = JsonConvert.DeserializeObject<GeoJsonData.GeoJson >(jsonString);
                foreach (GeoJsonData.Feature feature in geoJson.features)
                {
                    // フィーチャーを解析した結果をリストに追加
                    result.Add(ParseFeature(feature));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{filePaths[0]} の読み込みに失敗しました。 {e.Message}");
                return result;
            }

            return result;
        }

        /// <summary>
        /// featuresを解析
        /// </summary>
        /// <param name="feature"></param>
        private GisData ParseFeature(GeoJsonData.Feature feature)
        {
            var gisData = new GisData();
            
            // 属性情報セット
            foreach (KeyValuePair<string, object> property in feature.properties)
            {
                var key = property.Key.Trim();
                var value = property.Value.ToString().Trim();
                var atttibute = new KeyValuePair<string, string>(key, value);
                gisData.Attributes.Add(atttibute);
            }
            
            // 位置情報セット
            var coordinates = (JArray)feature.geometry.coordinates;
            gisData.WorldPoints = GetWorldPoints(feature.geometry.type, coordinates);
            return gisData;
        }

        /// <summary>
        /// ワールド座標を取得
        /// </summary>
        /// <param name="type"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private List<Vector3> GetWorldPoints(string type, JArray coordinates)
        {
            // 高さは0でセット
            var height = 0f;
            var result = new List<Vector3>();
            
            switch (type)
            {
                case "Point":
                    double[] pointCoords = coordinates.ToObject<double[]>();
                    var worldSpace = ConvertCoordinateToWorldSpace(pointCoords, height);
                    result.Add(worldSpace);
                    break;
                case "MultiPolygon":
                    double[][][][] multiPolygonCoordinates = coordinates.ToObject<double[][][][]>();
                    foreach (double[][][] polygonCoords in multiPolygonCoordinates)
                    {
                        foreach (var coords in polygonCoords)
                        {
                            result = result.Concat(ConvertCoordinates(coords, height)).ToList();
                        }
                    }
                    break;
                case "Polygon":
                    double[][][] polygonCoordinates = coordinates.ToObject<double[][][]>();
                    foreach (var coords in polygonCoordinates)
                    {
                        result = result.Concat(ConvertCoordinates(coords, height)).ToList();
                    }
                    break;
                case "LineString":
                    double[][] lineCoordinates = coordinates.ToObject<double[][]>();
                    result = ConvertCoordinates(lineCoordinates, height);
                    break;
                default:
                    Debug.LogError($"Unsupported geometry type: {type}");
                    break;
            }
            
            return result;
        }

        /// <summary>
        /// Coordinatesをワールド座標に変換
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private List<Vector3> ConvertCoordinates(double[][] coords, float height)
        {
            List<Vector3> worldCoords = new List<Vector3>();
            foreach (double[] coord in coords)
            {
                var worldPosition = ConvertCoordinateToWorldSpace(coord, height);
                worldCoords.Add(worldPosition);
            }
            return worldCoords;
        }

        private Vector3 ConvertCoordinateToWorldSpace(double[] coord, float height)
        {
            double3 position = new double3(coord[0], coord[1], height);
            CesiumGlobeAnchor cesiumGlobeAnchor = cesiumGlobeAnchorObject.GetComponent<CesiumGlobeAnchor>();

            if (cesiumGlobeAnchor != null)
            {
                cesiumGlobeAnchorObject.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = position;
            }
            return cesiumGlobeAnchorObject.transform.position;
        }
    }
}