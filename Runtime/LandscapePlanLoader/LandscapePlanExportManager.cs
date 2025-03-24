using System.Collections.Generic;
using UnityEngine;
using EGIS.ShapeFileLib;
using System.IO;
using UnityEditor;
using PLATEAU.CityInfo;
using CesiumForUnity;
using SFB;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// Shapeファイルへの書き出しを管理するクラス
    /// </summary>
    public sealed class LandscapeExportManager
    {
        /// <summary>
        /// フォルダ保存用のダイアログを開き、パスを取得するメソッド
        /// </summary>
        /// extension: 拡張子
        /// <returns>フォルダパス</returns>
        public string OpenShapeExportDialog()
        {
            var path = StandaloneFileBrowser.SaveFilePanel("保存先", "", "Shapefile", "shp");
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
            return null;
        }

        /// <summary>
        /// 景観区画データをSHPファイルとして保存するメソッド
        /// </summary>
        public void WriteShapeFile(string exportFilePath)
        {
            // 区画データ数
            int nblock = AreasDataComponent.GetPropertyCount();

            // 都市モデルを取得
            var cityModel = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
            if (cityModel == null)
            {
                Debug.LogError("CityModel is not found.");
                return;
            }

            var geoRef = GameObject.FindObjectOfType<CesiumGeoreference>();
            if (geoRef == null)
            {
                Debug.LogError("CesiumGeoreference is not found.");
                return;
            }
            GameObject positionMarkerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            positionMarkerSphere.transform.SetParent(geoRef.transform);
            positionMarkerSphere.AddComponent<CesiumGlobeAnchor>();

            Debug.Log("WriteShapeFile to : " + exportFilePath);
            DbfFieldDesc[] fields = new DbfFieldDesc[7];
            fields[0] = new DbfFieldDesc { FieldName = "ID", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[1] = new DbfFieldDesc { FieldName = "TYPE", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[2] = new DbfFieldDesc { FieldName = "AREANAME", FieldType = DbfFieldType.Character, FieldLength = 33, RecordOffset = 0 };
            fields[3] = new DbfFieldDesc { FieldName = "HEIGHT", FieldType = DbfFieldType.Character, FieldLength = 14, RecordOffset = 0 };
            fields[4] = new DbfFieldDesc { FieldName = "COLOR", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[5] = new DbfFieldDesc { FieldName = "POINT1", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };
            fields[6] = new DbfFieldDesc { FieldName = "POINT2", FieldType = DbfFieldType.Character, FieldLength = 128, RecordOffset = 0 };


            string exportBaseDirPath = Directory.GetParent(exportFilePath)?.FullName;
            if (string.IsNullOrEmpty(exportBaseDirPath))
            {
                Debug.LogError($"Export path is invalid. path = {exportFilePath}");
                return;
            }
            ShapeFileWriter sfw = ShapeFileWriter.CreateWriter(exportBaseDirPath, Path.GetFileNameWithoutExtension(exportFilePath), ShapeType.Polygon, fields);

            Debug.Log("nblock:" + nblock);
            for (int i = 0; i < nblock; i++)
            {
                AreaProperty areaProperty = AreasDataComponent.GetProperty(i);

                List<Vector3> vlist3D = areaProperty.PointData[0];
                int n = 0;
                PointD[] vertex = new PointD[vlist3D.Count];
                foreach (var v in vlist3D)
                {
                    var globeAnchor = positionMarkerSphere.GetComponent<CesiumGlobeAnchor>();
                    globeAnchor.transform.position = v;
                    globeAnchor.Sync();
                    var convertedPoint = positionMarkerSphere.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight;
                    vertex[n++] = new PointD(convertedPoint.x, convertedPoint.y);
                }
                
                string[] fielddata = new string[7];
                fielddata[0] = i.ToString();
                fielddata[1] = "PolygonArea";
                fielddata[2] = areaProperty.Name;
                fielddata[3] = areaProperty.LimitHeight.ToString();
                fielddata[4] = areaProperty.Color.r.ToString() + "," + areaProperty.Color.g.ToString() + "," + areaProperty.Color.b.ToString() + "," + areaProperty.Color.a.ToString();
                fielddata[5] = "0, 0";
                fielddata[6] = "0, 0";

                sfw.AddRecord(vertex, vertex.Length, fielddata);

            }

            sfw.Close();
        }
    }
}

