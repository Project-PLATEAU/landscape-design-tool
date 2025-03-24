using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class ProjectSettingListUI
    {
        private VisualTreeAsset projectItemTreeAsset;
        private ScrollView scrollView;
        private VisualElement projectListContainer;
        
        public bool IsShow => projectListContainer.style.display == DisplayStyle.Flex;
        
        public UnityEvent<string> OnSelect = new ();
        public UnityEvent<string> OnDelete = new ();
        public UnityEvent<ProjectData> OnSave = new ();
        public UnityEvent<string> OnRename = new ();
        public UnityEvent<string> OnUp = new ();
        public UnityEvent<string> OnDown = new ();
        
        public ProjectSettingListUI(VisualElement element)
        {
            projectListContainer = element.Q<VisualElement>("Project_List_Container");
            scrollView = projectListContainer.Q<ScrollView>("Project_List");
            var projectBlank = element.Q<VisualElement>("List_Project_blank");
            projectItemTreeAsset = Resources.Load<VisualTreeAsset>("List_Project");
            
            Show(false);
        }
        
        public void Show(bool isShow)
        {
            projectListContainer.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public void Add(ProjectData projectData)
        {
            var clone = projectItemTreeAsset.CloneTree();
            var label = clone.Q<Label>("ProjectName");
            label.text = projectData.projectName;
            label.name = projectData.projectID;
            
            // プロジェクトを選択
            clone.Q<Button>("Btn_Project").clicked += () =>
            {
                if (ProjectSaveDataManager.ProjectSetting.IsCurrentProject(projectData.projectID))
                {
                    return;
                }
                OnSelect.Invoke(projectData.projectID);
                Show(false);
            };
            
            // 保存
            clone.Q<Button>("SaveButton").clicked += () =>
            {
                // ファイルに保存
                OnSave.Invoke(projectData);
            };
            
            // 削除
            clone.Q<Button>("DeleteButton").clicked += () =>
            {
                OnDelete.Invoke(projectData.projectID);
            };
            
            // リネーム
            clone.Q<Button>("RenameButton").clicked += () =>
            {
                // ポップアップを表示
                OnRename.Invoke(projectData.projectID);
            };
            
            // 上に移動
            var upButton = clone.Q<Button>("UpButton");
            upButton.clicked += () =>
            {
                OnUp.Invoke(projectData.projectID);
            };
            
            // 下に移動
            var downButton = clone.Q<Button>("DownButton");
            downButton.clicked += () =>
            {
                OnDown.Invoke(projectData.projectID);
            };
            
            // リストの位置に応じてボタンの表示/非表示を制御
            if (ProjectSaveDataManager.ProjectSetting.IsTopLayer(projectData.projectID))
            {
                upButton.style.visibility = Visibility.Hidden;
            }
            if (ProjectSaveDataManager.ProjectSetting.IsBottomLayer(projectData.projectID))
            {
                downButton.style.visibility = Visibility.Hidden;
            }
            
            scrollView.Add(clone);
        }
        
        public void Rename(string projectID, string newProjectName)
        {
            var label = scrollView.Q<Label>(projectID);
            if (label.text.Contains(("*")))
            {
                newProjectName = $"* {newProjectName}";
            }
            label.text = newProjectName;
        }
        
        public void Delete(string projectID)
        {
            var label = scrollView.Q<Label>(projectID);
            scrollView.Remove(label.parent.parent);
        }
        
        public void Edit(string projectID)
        {
            var label = scrollView.Q<Label>(projectID);
            if (label == null)
            {
                return;
            }
            if (label.text.Contains(("*")))
            {
                return;
            }
            label.text = $"* {label.text}";
        }

        public void Save(string projectID)
        {
            var label = scrollView.Q<Label>(projectID);
            if (label.text.Contains(("*")))
            {
                label.text = label.text.Replace("* ", "");
            }
        }

        public void Clear()
        {
            while (scrollView.childCount > 0)
            {
                scrollView.RemoveAt(0);
            }
        }
    }
}