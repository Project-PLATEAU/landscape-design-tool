using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ToolBox.Serialization;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace Landscape2.Runtime
{
    public class LineOfSightSubscribeSaveSystem
    {
        private LineOfSightDataComponent lineOfSightDataComponent;
        private LineOfSightUI lineOfSightUI;
        private SaveSystem saveSystem;
        private Sprite viewPointIconSprite;
        private Sprite landmarkIconSprite;
        private Landmark landmark;
        private ViewPoint viewPoint;
        
        public LineOfSightSubscribeSaveSystem(
            SaveSystem saveSystemInstance,
            LineOfSightDataComponent lineOfSightDataComponentInstance,
            LineOfSightUI lineOfSightUIInstance,
            ViewPoint viewPoint,
            Landmark landmark)
        {
            lineOfSightDataComponent = lineOfSightDataComponentInstance;
            lineOfSightUI = lineOfSightUIInstance;
            saveSystem = saveSystemInstance;
            this.viewPoint = viewPoint;
            this.landmark = landmark;
            
            LoadPointIcon();
            SetEvents();
        }

        private async void LoadPointIcon()
        {
            AsyncOperationHandle<Sprite> viewPointIconHandle = Addressables.LoadAssetAsync<Sprite>("ViewPointIcon");
            viewPointIconSprite = await viewPointIconHandle.Task;
            AsyncOperationHandle<Sprite> landmarkIconHandle = Addressables.LoadAssetAsync<Sprite>("LandmarkIcon");
            landmarkIconSprite = await landmarkIconHandle.Task;
        }

        private void SetEvents()
        {
            saveSystem.SaveEvent += SaveDict;
            saveSystem.LoadEvent += LoadViewPoint;
            saveSystem.LoadEvent += LoadLandmark;
            saveSystem.LoadEvent += LoadAnalyzeViewPoint;
            saveSystem.LoadEvent += LoadAnalyzeLandmark;
            saveSystem.DeleteEvent += OnDelete;
            saveSystem.ProjectChangedEvent += OnProjectChanged;
        }

        /// <summary>
        /// 各データを取得し、保存する
        /// </summary>
        private void SaveDict(string projectID)
        {
            var viewPoints = lineOfSightDataComponent.ViewPointDatas
                .Where(data => data.IsProject(projectID))
                .ToList();
            
            var landmarks = lineOfSightDataComponent.LandmarkDatas
                .Where(data => data.IsProject(projectID))
                .ToList();
            
            var analyzeViewPoints = lineOfSightDataComponent.AnalyzeViewPointDatas
                .Where(data => data.IsProject(projectID))
                .ToList();
            
            var analyzeLandmarks = lineOfSightDataComponent.AnalyzeLandmarkDatas
                .Where(data => data.IsProject(projectID))
                .ToList();
            
            DataSerializer.Save(LineOfSightViewPointData.SaveKeyName, viewPoints);
            DataSerializer.Save(LineOfSightLandMarkData.SaveKeyName, landmarks);
            DataSerializer.Save(LineOfSightAnalyzeViewPointData.SaveKeyName, analyzeViewPoints);
            DataSerializer.Save(LineOfSightAnalyzeLandmarkData.SaveKeyName, analyzeLandmarks);
        }
        
        private void LoadViewPoint(string projectID)
        {
            var viewPointMarkers = GameObject.Find("ViewPointMarkers");
            var viewPointDatas = DataSerializer.Load<List<LineOfSightViewPointData>>(LineOfSightViewPointData.SaveKeyName);
            foreach (var data in viewPointDatas)
            {
                if (lineOfSightDataComponent.ViewPointDatas.Exists(point => point.Name == data.Name))
                {
                    // 既に存在している場合は命名変更
                    data.Rename(lineOfSightDataComponent.ViewPointDatas
                        .Select(point => point.Name)
                        .ToList());
                }
                lineOfSightDataComponent.ViewPointDatas.Add(data);
                lineOfSightUI.CreateViewPointButton(data.Name);
                
                // ポイントの生成
                var point = viewPoint.GeneratePointMarker(data.viewPoint.pointPos, data.viewPoint.yOffset);
                point.transform.parent = viewPointMarkers.transform;
                point.name = data.Name;
                
                // プロジェクトに通知
                data.Add(projectID, false);
            }
        }
        
        private void LoadLandmark(string projectID)
        {
            var landmarkMarkers = GameObject.Find("LandmarkMarkers");
            var landmarkDatas = DataSerializer.Load<List<LineOfSightLandMarkData>>(LineOfSightLandMarkData.SaveKeyName);
            foreach (var data in landmarkDatas)
            {
                if (lineOfSightDataComponent.LandmarkDatas.Exists(point => point.Name == data.Name))
                {
                    // 既に存在している場合は命名変更
                    data.Rename(lineOfSightDataComponent.LandmarkDatas
                        .Select(point => point.Name)
                        .ToList());
                }
                
                lineOfSightDataComponent.LandmarkDatas.Add(data);
                lineOfSightUI.CreateLandmarkButton(data.Name);
                
                // ポイントの生成
                var point = landmark.GeneratePointMarker(data.landmark.pointPos, data.landmark.yOffset);
                point.transform.parent = landmarkMarkers.transform;
                point.name = data.Name;

                // プロジェクトに通知
                data.Add(projectID, false);
            }
        }
        
        private void LoadAnalyzeViewPoint(string projectID)
        {
            var analyzeViewPointDatas = DataSerializer.Load<List<LineOfSightAnalyzeViewPointData>>(LineOfSightAnalyzeViewPointData.SaveKeyName);
            foreach (var data in analyzeViewPointDatas)
            {
                if (lineOfSightDataComponent.AnalyzeViewPointDatas.Exists(point => point.Name == data.Name))
                {
                    // 既に存在している場合は命名変更
                    data.Rename(lineOfSightDataComponent.AnalyzeViewPointDatas
                        .Select(point => point.Name)
                        .ToList());
                }
                
                lineOfSightDataComponent.AnalyzeViewPointDatas.Add(data);
                lineOfSightUI.CreateAnalyzeViewPointButton(data.analyzeViewPoint, data.Name);
                
                // プロジェクトに通知
                data.Add(projectID, false);
            }
        }
        
        private void LoadAnalyzeLandmark(string projectID)
        {
            var analyzeLandmarkDatas = DataSerializer.Load<List<LineOfSightAnalyzeLandmarkData>>(LineOfSightAnalyzeLandmarkData.SaveKeyName);
            foreach (var data in analyzeLandmarkDatas)
            {
                if (lineOfSightDataComponent.AnalyzeLandmarkDatas.Exists(point => point.Name == data.Name))
                {
                    // 既に存在している場合は命名変更
                    data.Rename(lineOfSightDataComponent.AnalyzeLandmarkDatas
                        .Select(point => point.Name)
                        .ToList());
                }
                
                lineOfSightDataComponent.AnalyzeLandmarkDatas.Add(data);
                lineOfSightUI.CreateAnalyzeLandmarkButton(data.analyzeLandmark, data.Name);
                
                // プロジェクトに通知
                data.Add(projectID, false);
            }
        }
        
        private void OnDelete(string projectID)
        {
            lineOfSightDataComponent.ViewPointDatas
                .Where(data => data.IsProject(projectID, false))
                .ToList()
                .ForEach(data =>
                {
                    lineOfSightUI.DeletePoint(LineOfSightType.viewPoint, data.Name);
                    lineOfSightDataComponent.ViewPointDatas.Remove(data);
                });
            
            lineOfSightDataComponent.LandmarkDatas
                .Where(data => data.IsProject(projectID, false))
                .ToList()
                .ForEach(data =>
                {
                    lineOfSightUI.DeletePoint(LineOfSightType.landmark, data.Name);
                    lineOfSightDataComponent.LandmarkDatas.Remove(data);
                });
            
            lineOfSightDataComponent.AnalyzeViewPointDatas
                .Where(data => data.IsProject(projectID, false))
                .ToList()
                .ForEach(data =>
                {
                    lineOfSightUI.DeleteAnalyzeViewPoint(data.Name);
                    lineOfSightDataComponent.AnalyzeViewPointDatas.Remove(data);
                });
            
            lineOfSightDataComponent.AnalyzeLandmarkDatas
                .Where(data => data.IsProject(projectID, false))
                .ToList()
                .ForEach(data =>
                {
                    lineOfSightUI.DeleteAnalyzeLandmark(data.Name);
                    lineOfSightDataComponent.AnalyzeLandmarkDatas.Remove(data);
                });
        }

        private void OnProjectChanged(string projectID)
        {
            // 編集中であれば閉じる
            lineOfSightUI.TryCloseEditView(LineOfSightType.viewPoint);
            lineOfSightUI.TryCloseEditView(LineOfSightType.landmark);
            lineOfSightUI.TryCloseEditView(LineOfSightType.analyzeViewPoint);
            lineOfSightUI.TryCloseEditView(LineOfSightType.analyzeLandmark);
        }
    }
}