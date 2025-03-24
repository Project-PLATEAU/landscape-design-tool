using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using ToolBox.Serialization;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

using UnityEngine.UIElements;
using Landscape2.Runtime.UiCommon;
using Landscape2.Runtime.CameraPositionMemory;

using SFB;

namespace Landscape2.Runtime
{
    public class SaveSystem : ISubComponent
    {
        private VisualElement projectManagementUI;

        public event Action<string> SaveEvent = delegate { };
        public event Action<string> LoadEvent = delegate { };
        public event Action<string> DeleteEvent = delegate { };
        public event Action<string> ProjectChangedEvent = delegate { };
        public event Action LayerChangedEvent = delegate { };

        public SaveSystem(VisualElement globalNavi)
        {
            // UIの設定
            Button saveButton = globalNavi.Q<Button>("merge_project_button");
            Button loadButton = globalNavi.Q<Button>("Import_button");

            saveButton.clicked += () => SaveProject();
            loadButton.clicked += LoadProject;
        }
        
        public void SaveProject(string projectID = "", string projectName = "")
        {
            var path = "";
#if UNITY_EDITOR && UNITY_STANDALONE_OSX 
            // NOTE: macで開発時用にEditorのファイル選択ダイアログを表示
             path = UnityEditor.EditorUtility.SaveFilePanel("Create File", "", projectName, "data");
#else
             path = StandaloneFileBrowser.SaveFilePanel("Create File", "", projectName, "data");
#endif
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            DataSerializer._savePath = path;

            SaveEvent(projectID);

            DataSerializer.SaveFile();

            // 完了ポップアップ
            ModalUI.ShowModal("プロジェクト保存", "プロジェクトを保存しました。", false, false);
            
            Debug.Log($"Project saved. {projectID}.");
        }
        
        void LoadProject()
        {
            string[] paths;
#if UNITY_EDITOR && UNITY_STANDALONE_OSX 
            // NOTE: macで開発時用にEditorのファイル選択ダイアログを表示
            var openFile = UnityEditor.EditorUtility.OpenFilePanel("Open File", "", "data");
            paths = new string[] { openFile };
#else
            paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "data", false);
#endif
            string path = "";
            if (paths.Length > 0)
            {
                path = paths[0];
            }
            
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            DataSerializer._savePath = path;

            DataSerializer.LoadFile();
            var projectName = System.IO.Path.GetFileNameWithoutExtension(path);
            var project = ProjectSaveDataManager.ProjectSetting.Add(projectName);
            
            // 先に現在のプロジェクトに設定
            ProjectSaveDataManager.ProjectSetting.SetCurrentProject(project.projectID);
            
            // IDで通知
            LoadEvent(project.projectID);

            // 完了ポップアップ
            ModalUI.ShowModal("プロジェクト読み込み", "プロジェクトを読み込みしました。", false, false);
            
            Debug.Log("Project loaded.");
        }

        public void ResetLoadEvent()
        {
            LoadEvent = null;
        }
        
        public void Delete(string projectID)
        {
            DeleteEvent(projectID);
        }
        
        public void SetProject(string projectID)
        {
            ProjectChangedEvent(projectID);
        }

        public void SetLayer()
        {
            LayerChangedEvent();
        }

        public void OnEnable()
        {
        }
        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
        }
        public void OnDisable()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
