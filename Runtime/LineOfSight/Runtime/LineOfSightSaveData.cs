using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime
{
    [Serializable]
    public abstract class LineOfSightSaveDataBase
    {
        [SerializeField]
        public string ID { get; private set; }
        [SerializeField]
        public string Name { get; private set; }

        protected LineOfSightSaveDataBase(string name)
        {
            this.Name = name;
            Add();
        }
        
        public void Add(string projectID = "", bool isEdit = true)
        {
            ID = Guid.NewGuid().ToString();
            
            // プロジェクトに追加
            ProjectSaveDataManager.Add(ProjectSaveDataType.LineOfSight, ID, projectID, isEdit);
        }
        
        public void Delete()
        {
            // プロジェクトから削除
            ProjectSaveDataManager.Delete(ProjectSaveDataType.LineOfSight, ID);
        }

        public bool IsExist(string name)
        {
            return this.Name == name;
        }

        public bool IsProject(string projectID, bool isCheckEditMode = true)
        {
            if (string.IsNullOrEmpty(projectID))
            {
                // 結合保存であればtrue
                return true;
            }

            return ProjectSaveDataManager.TryCheckData(
                ProjectSaveDataType.LineOfSight,
                projectID,
                ID,
                isCheckEditMode);
        }

        public void Rename(List<string> names, int index = 0)
        {
            // 名称が被らないようにRename
            var newName = this.Name + $"_{index}";
            if (names.Contains(newName))
            {
                Rename(names, index + 1);
                return;
            }
            this.Name = newName;
        }
    }

    [Serializable]
    public class LineOfSightViewPointData : LineOfSightSaveDataBase
    {
        public static string SaveKeyName = "ViewPoint";
        
        [SerializeField]
        public LineOfSightDataComponent.PointData viewPoint;
        
        public LineOfSightViewPointData(string name, LineOfSightDataComponent.PointData viewPoint) : base(name)
        {
            this.viewPoint = viewPoint;
        }
    }

    [Serializable]
    public class LineOfSightLandMarkData : LineOfSightSaveDataBase
    {
        public static string SaveKeyName = "Landmark";
        
        [SerializeField]
        public LineOfSightDataComponent.PointData landmark;
        
        public LineOfSightLandMarkData(string name, LineOfSightDataComponent.PointData landmark) : base(name)
        {
            this.landmark = landmark;
        }
    }

    [Serializable]
    public class LineOfSightAnalyzeViewPointData : LineOfSightSaveDataBase
    {
        public static string SaveKeyName = "AnalyzeViewPoint";

        [SerializeField]
        public AnalyzeViewPointElements analyzeViewPoint;
        
        public LineOfSightAnalyzeViewPointData(string name, AnalyzeViewPointElements analyzeViewPoint) : base(name)
        {
            this.analyzeViewPoint = analyzeViewPoint;
        }
    }

    [Serializable]
    public class LineOfSightAnalyzeLandmarkData : LineOfSightSaveDataBase
    {
        public static string SaveKeyName = "AnalyzeLandmark";

        [SerializeField]
        public AnalyzeLandmarkElements analyzeLandmark;
        
        public LineOfSightAnalyzeLandmarkData(string name, AnalyzeLandmarkElements analyzeLandmark) : base(name)
        {
            this.analyzeLandmark = analyzeLandmark;
        }
    }
}