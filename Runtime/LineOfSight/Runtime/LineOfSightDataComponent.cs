using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using UnityEngine;

namespace Landscape2.Runtime
{
    /// <summary>
    /// データを管理するクラス
    /// </summary>
    public class LineOfSightDataComponent
    {
        [Serializable]
        public class PointData
        {
            [SerializeField]
            public Vector3 pointPos;
            
            [SerializeField]
            public float yOffset;
        }

        public List<LineOfSightViewPointData> ViewPointDatas { get; private set; } = new();
        public List<LineOfSightLandMarkData> LandmarkDatas { get; private set; } = new();
        public List<LineOfSightAnalyzeViewPointData> AnalyzeViewPointDatas { get; private set; } = new();
        public List<LineOfSightAnalyzeLandmarkData> AnalyzeLandmarkDatas { get; private set; } = new();
        
        private LineOfSightUI lineOfSightUI;

        /// <summary>
        /// 視点場、眺望対象を追加する
        /// offset高さも保持する様にした
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pointName"></param>
        /// <param name="pointData"></param>
        /// <returns></returns>
        public bool AddPointData(LineOfSightType type, string pointName, PointData pointData)
        {
            if (type == LineOfSightType.viewPoint)
            {
                if (ViewPointDatas.Any(data => data.IsExist(pointName)))
                {
                    return false;
                }
                ViewPointDatas.Add(new LineOfSightViewPointData(pointName, pointData));
                return true;
            }
            else if (type == LineOfSightType.landmark)
            {
                if (LandmarkDatas.Any(data => data.IsExist(pointName)))
                {
                    return false;
                }
                LandmarkDatas.Add(new LineOfSightLandMarkData(pointName, pointData));
                return true;
            }
            return false;

        }

        /// <summary>
        /// 視点場解析結果を追加する
        /// </summary>
        public bool AddAnalyzeViewPoinData(string keyName, AnalyzeViewPointElements elements)
        {
            if (AnalyzeViewPointDatas.Any(data => data.IsExist(keyName)))
            {
                Debug.LogWarning($"analyzeViewPointDataDict already has Key : {keyName}");
                return false;
            }
            
            AnalyzeViewPointDatas.Add(new LineOfSightAnalyzeViewPointData(keyName, elements));
            return true;
        }

        public bool RemoveAnalyzeViewPoinData(string keyName)
        {
            if (AnalyzeViewPointDatas.Any(data => data.IsExist(keyName)))
            {
                var analyzeViewPointData = AnalyzeViewPointDatas
                    .First(data => data.IsExist(keyName));
                analyzeViewPointData?.Delete();
                AnalyzeViewPointDatas.Remove(analyzeViewPointData);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 眺望対象解析結果を追加する
        /// </summary>
        public bool AddAnalyzeLandmarkData(string keyName, AnalyzeLandmarkElements elements)
        {
            if (AnalyzeLandmarkDatas.Any(data => data.IsExist(keyName)))
            {
                Debug.LogWarning($"analyzeLandmarkDataDict already has Key : {keyName}");
                return false;
            }
            
            AnalyzeLandmarkDatas.Add(new LineOfSightAnalyzeLandmarkData(keyName, elements));
            return true;
        }
        
        public bool RemoveAnalyzeLandmarkData(string keyName)
        {
            if (AnalyzeLandmarkDatas.Any(data => data.IsExist(keyName)))
            {
                var analyzeLandmarkData = AnalyzeLandmarkDatas
                    .First(data => data.IsExist(keyName));
                analyzeLandmarkData?.Delete();
                AnalyzeLandmarkDatas.Remove(analyzeLandmarkData);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 視点場、眺望対象を削除する
        /// </summary>
        public (bool isRemoved, List<string> removedAnalyzeKeyNameList) RemovePointElement(LineOfSightType lineOfSightType, string keyName)
        {
            var removedAnalyzeKeyNameList = new List<string>();
            if (lineOfSightType == LineOfSightType.viewPoint)
            {
                ViewPointDatas
                    .Where(data => data.IsExist(keyName))
                    .Select(data =>
                    {
                        removedAnalyzeKeyNameList.Add(data.Name);
                        data.Delete();
                        return data;
                    })
                    .ToList()
                    .ForEach(data => ViewPointDatas.Remove(data));
                
                return (true, removedAnalyzeKeyNameList);
            }
            else if (lineOfSightType == LineOfSightType.landmark)
            {
                if (LandmarkDatas.Any(data => data.IsExist(keyName)))
                {
                    var landmarkData = LandmarkDatas
                        .First(data => data.IsExist(keyName));

                    landmarkData?.Delete();
                    LandmarkDatas.Remove(landmarkData);
                    
                    // 削除した眺望対象が視点場解析結果に含まれていた時の処理
                    AnalyzeViewPointDatas
                        .Where(data => data.analyzeViewPoint.endPosName == keyName)
                        .Select(data =>
                        {
                            removedAnalyzeKeyNameList.Add(data.Name);
                            data.Delete();
                            return data;
                        })
                        .ToList()
                        .ForEach(data => AnalyzeViewPointDatas.Remove(data));
                    
                    // 削除した眺望対象が眺望対象解析結果に含まれていた時の処理
                    AnalyzeLandmarkDatas
                        .Where(data => data.analyzeLandmark.startPosName == keyName)
                        .Where(data => !removedAnalyzeKeyNameList.Contains(keyName))
                        .Select(data =>
                        {
                            removedAnalyzeKeyNameList.Add(data.Name);
                            data.Delete();
                            return data;
                        })
                        .ToList()
                        .ForEach(data => AnalyzeLandmarkDatas.Remove(data));
                    
                    return (true, removedAnalyzeKeyNameList);
                }
            }
            return (false, removedAnalyzeKeyNameList);
        }
    }
}
