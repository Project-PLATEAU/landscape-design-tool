using Landscape2.Runtime.UiCommon;
using System.Linq;
using ToolBox.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ProjectSettingUI
    {
        private ProjectRegistModalUI projectRegistModalUI;
        private ProjectSettingListUI projectSettingListUI;
        private ProjectSettingEditModeUI projectSettingEditModeUI;
        private Label currentProjectName;
        private Button selectProjectButton;
        private Button addProjectButton;
        private SaveSystem saveSystem;
        
        public ProjectSettingUI(VisualElement element, SaveSystem saveSystem)
        {
            projectRegistModalUI = new (element);
            projectSettingListUI = new (element);
            projectSettingEditModeUI = new (element);
            currentProjectName = element.Q<Label>("SelectProject_Name");
            selectProjectButton = element.Q<Button>("Btn_SelectProject");
            addProjectButton = element.Q<Button>("New_project_button");
            this.saveSystem = saveSystem;
            
            Initialize();
            RegisterEvents(element);
        }

        private void Initialize()
        {
            // デフォルトプロジェクトを追加
            var defaultProjectID = ProjectSaveDataManager.ProjectSetting.GetDefaultProject().projectID;
            Add(defaultProjectID);
            SetCurrentProject(defaultProjectID);

            Show(false);
        }
        
        public void Show(bool isShow)
        {
            projectSettingListUI.Show(isShow);
        }

        private void RegisterEvents(VisualElement element)
        {
            // Load/Saveのみここで受け取る
            saveSystem.LoadEvent += OnLoad;
            saveSystem.SaveEvent += (projectID) =>
            {
                if (!string.IsNullOrEmpty(projectID))
                {
                    return;
                }

                // 全て保存
                foreach (var projectData in ProjectSaveDataManager.ProjectSetting.ProjectList)
                {
                    projectSettingListUI.Save(projectData.projectID);
                }
            };
            
            // 編集中表示更新
            ProjectSaveDataManager.OnEditProject.AddListener((projectID) =>
            {
                projectSettingListUI.Edit(projectID);
            });

            // list ui
            projectSettingListUI.OnSelect.AddListener(OnProjectChanged);
            projectSettingListUI.OnSave.AddListener((projectData) =>
            {
                saveSystem.SaveProject(projectData.projectID, projectData.projectName);
                projectSettingListUI.Save(projectData.projectID);
            });
            projectSettingListUI.OnDelete.AddListener((projectID) =>
            {

                projectSettingListUI.Delete(projectID);
                ProjectSaveDataManager.ProjectSetting.Remove(projectID);
                
                // 各システムに通知
                saveSystem.Delete(projectID);

                if (!ProjectSaveDataManager.ProjectSetting.ProjectList.Any())
                {
                    // プロジェクトがなくならないように、デフォルトプロジェクトを追加
                    var project = ProjectSaveDataManager.ProjectSetting.CreateDefaultProject();
                    Add(project.projectID);
                    SetCurrentProject(project.projectID);
                    
                    // プロジェクト変更を通知
                    saveSystem.SetProject(project.projectID);
                }
                else if (ProjectSaveDataManager.ProjectSetting.IsCurrentProject(projectID))
                {
                    var firstProject = ProjectSaveDataManager.ProjectSetting.ProjectList.First();
                    SetCurrentProject(firstProject.projectID);
                }
                
                RefreshProjectList();

                // 完了ポップアップ
                ModalUI.ShowModal("プロジェクト削除", "プロジェクトを削除しました。", false, false);
            });
            projectSettingListUI.OnRename.AddListener((projectID) =>
            {
                var projectData = ProjectSaveDataManager.ProjectSetting.GetProject(projectID);
                
               // モーダル表示
                projectRegistModalUI.Show(true, projectData.projectName, (newProjectName) =>
                {
                    // 登録時
                    ProjectSaveDataManager.ProjectSetting.Rename(projectID, newProjectName);
                    projectSettingListUI.Rename(projectID, newProjectName);
                    if (ProjectSaveDataManager.ProjectSetting.IsCurrentProject(projectID))
                    {
                        SetCurrentProjectName(newProjectName);
                    }

                    // 完了ポップアップ
                    ModalUI.ShowModal("プロジェクト名編集", "プロジェクト名を変更しました。", true, false);
                });
            });
            
            // 上に移動
            projectSettingListUI.OnUp.AddListener((projectID) =>
            {
                ProjectSaveDataManager.ProjectSetting.MoveLayerUp(projectID);
                RefreshProjectList();
                saveSystem.SetLayer();
            });

            // 下に移動
            projectSettingListUI.OnDown.AddListener((projectID) =>
            {
                ProjectSaveDataManager.ProjectSetting.MoveLayerDown(projectID);
                RefreshProjectList();
                saveSystem.SetLayer();
            });
            
            // 追加ボタン
            addProjectButton.clicked += () =>
            {
                projectRegistModalUI.Show(true, "", (projectName) =>
                {
                    var projectData = ProjectSaveDataManager.ProjectSetting.Add(projectName);
                    Add(projectData.projectID);
                    RefreshProjectList();
                
                    // 完了ポップアップ
                    ModalUI.ShowModal("プロジェクト新規作成", "新規プロジェクトを追加しました。", true, false);
                });
            };
            
            // 選択
            selectProjectButton.clicked += () =>
            {
                Show(!projectSettingListUI.IsShow);
            };
            
            // EditMode
            projectSettingEditModeUI.OnEditModeChanged.AddListener((isEdit) =>
            {
                // プロジェクト切り替え通知をして、モードを切り変える
                saveSystem.SetProject(
                    ProjectSaveDataManager.ProjectSetting.CurrentProject.projectID);
            });
        }
        
        private void Add(string projectID)
        {
            var projectData = ProjectSaveDataManager.ProjectSetting.GetProject(projectID);
            projectSettingListUI.Add(projectData);
        }
        
        private void OnLoad(string projectID)
        {
            Add(projectID);
            
            // 現在のプロジェクトに設定
            SetCurrentProject(projectID);
        
            RefreshProjectList();
        }
        
        private void OnProjectChanged(string projectID)
        {
            var currentProject = ProjectSaveDataManager.ProjectSetting.CurrentProject;
            if (currentProject.projectID == projectID)
            {
                return;
            }
            
            SetCurrentProject(projectID);
            
            // プロジェクト変更を通知
            saveSystem.SetProject(projectID);
        }
        
        private void SetCurrentProject(string projectID)
        {
            SetCurrentProjectName(ProjectSaveDataManager.ProjectSetting.GetProject(projectID).projectName);
            ProjectSaveDataManager.ProjectSetting.SetCurrentProject(projectID);
        }
        
        private void SetCurrentProjectName(string projectName)
        {
            currentProjectName.text = projectName;
        }

        private void RefreshProjectList()
        {
            // リストをクリア
            projectSettingListUI.Clear();
            
            // レイヤー順でプロジェクトを追加
            var sortedProjects = ProjectSaveDataManager.ProjectSetting.ProjectList
                .OrderBy(x => x.layer)
                .ToList();
                
            foreach (var project in sortedProjects)
            {
                Add(project.projectID);
            }
        }
    }
}