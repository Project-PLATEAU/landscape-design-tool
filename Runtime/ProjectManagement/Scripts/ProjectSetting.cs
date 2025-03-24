using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace Landscape2.Runtime
{
    public class ProjectData
    {
        public string projectName;
        public string projectID;
        public int layer;  // レイヤー情報
        
        public bool IsDefault => projectName == ProjectSetting.DefaultProjectName;
    }
    
    public class ProjectSetting
    {
        public static string DefaultProjectName = "プロジェクト01";
        
        private ProjectData currentProject;
        public ProjectData CurrentProject => currentProject;
        
        private List<ProjectData> projectList = new();
        public List<ProjectData> ProjectList => projectList;

        // 編集モード / 閲覧モード
        private bool isEditMode = true;
        public bool IsEditMode => isEditMode;
        
        public ProjectSetting()
        {
            currentProject = CreateDefaultProject();
        }
        
        public ProjectData CreateDefaultProject()
        {
            return Add(DefaultProjectName);
        }
        
        public ProjectData Add(string projectName)
        {
            var projectData = new ProjectData
            {
                projectName = projectName,
                projectID = System.Guid.NewGuid().ToString(),
                layer = projectList.Count + 1
            };
            projectList.Add(projectData);
            return projectData;
        }
        
        public void Remove(string projectID)
        {
            projectList.RemoveAll(x => x.projectID == projectID);
        }
        
        public void SetCurrentProject(string projectID)
        {
            currentProject = projectList.First(x => x.projectID == projectID);
        }
        
        public bool IsCurrentProject(string projectID)
        {
            return currentProject.projectID == projectID;
        }
        
        public ProjectData GetProject(string projectID)
        {
            return projectList.First(x => x.projectID == projectID);
        }

        public ProjectData GetDefaultProject()
        {
            return projectList.First(x => x.IsDefault);
        }

        public void Rename(string projectID, string newName)
        {
            if (projectList.All(x => x.projectID != projectID))
            {
                return;
            }
            
            var project = projectList.First(x => x.projectID == projectID);
            project.projectName = newName;
        }

        public void SetEditMode(bool isEdit)
        {
            isEditMode = isEdit;
        }

        /// <summary>
        /// プロジェクトが最上位レイヤーかどうかを判定
        /// </summary>
        public bool IsTopLayer(string projectID)
        {
            var project = GetProject(projectID);
            return project.layer == 1;
        }

        /// <summary>
        /// プロジェクトが最下位レイヤーかどうかを判定
        /// </summary>
        public bool IsBottomLayer(string projectID)
        {
            var project = GetProject(projectID);
            return project.layer == projectList.Count;
        }

        /// <summary>
        /// プロジェクトのレイヤーを1つ上に移動
        /// </summary>
        public void MoveLayerUp(string projectID)
        {
            var project = GetProject(projectID);
            
            // 現在のレイヤーより小さい値を持つプロジェクトの中から、最も近いレイヤーのプロジェクトを取得
            var upperProject = projectList
                .Where(x => x.layer < project.layer)
                .OrderByDescending(x => x.layer)
                .FirstOrDefault();
                
            if (upperProject != null)
            {
                // レイヤー値を交換
                upperProject.layer++;
                project.layer--;
            }            
        }

        /// <summary>
        /// プロジェクトのレイヤーを1つ下に移動
        /// </summary>
        public void MoveLayerDown(string projectID)
        {
            var project = GetProject(projectID);
            
            // 現在のレイヤーより大きい値を持つプロジェクトの中から、最も近いレイヤーのプロジェクトを取得
            var lowerProject = projectList
                .Where(x => x.layer > project.layer)
                .OrderBy(x => x.layer)
                .FirstOrDefault();
                
            if (lowerProject != null)
            {
                // レイヤー値を交換
                lowerProject.layer--;
                project.layer++;
            }
        }
    }
}