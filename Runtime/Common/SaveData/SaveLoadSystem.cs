using System;
using System.Collections.Generic;
using System.Linq;
using ToolBox.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.Common
{
    /// <summary>
    /// SaveLoadSystemで扱うためのインターフェイス
    /// </summary>
    public interface ISaveData 
    {
        public string ID { get; }

    }

    /// <summary>
    /// 実装参考用のサンプル
    /// </summary>
    [Serializable]
    public class SampleData : ISaveData
    {
        string ISaveData.ID => ID;

        [SerializeField]
        public readonly string ID;

        [SerializeField]
        public readonly Vector3 vec;

        [SerializeField]
        public readonly float distance;

        public SampleData(string id, Vector3 vec, float distance)
        {
            this.ID = id;
            this.vec = vec;
            this.distance = distance;
        }

    }

    /// <summary>
    /// プロジェクトID
    /// データIDとプロジェクトIDの型が同一かつ名前が紛らわしい所が多いのでこのインターフェイスを作成
    /// </summary>
    public interface IProjectID
    {
        string ID { get; }
    }

    /// <summary>
    /// プロジェクトデータの扱いに合わせたセーブロードを扱うためのシステムのインターフェイス
    /// </summary>
    /// <typeparam name="_T"></typeparam>
    public interface ISaveLoadSystem<_T>
    {
        // データを追加
        bool TryAddData(_T data);
        // データを削除
        bool TryRemoveData(string id);
        // 登録済みデータを更新
        void Update(_T data);

        // 読み込み時
        event Action<IProjectID, List<_T>> LoadCallback;
        // プロジェクト削除時
        event Action<IProjectID, List<_T>> DeleteCallback;
        // プロジェクト変更時
        event Action<IProjectID, List<_T>, List<_T>> ProjectChangeCallback;

    }

    /// <summary>
    /// プロジェクトの扱いに合わせたセーブロードを扱うためのシステム
    /// このクラスもしくは派生クラスをnewして　継承しているインターフェイスで保持するのを想定
    /// </summary>
    /// <typeparam name="_T"></typeparam>
    public class SaveLoadSystem<_T> : ISaveLoadSystem<_T>
        where _T : ISaveData
    {

        private class ProjectID :  IProjectID 
        {
            private string id;
            public ProjectID(string id)
            {
                this.id = id;
            }

            string IProjectID.ID => id;
        }



        // セーブデータ
        private List<_T> saveDatas = new();

        // プロジェクトセーブデータのタイプ
        private ProjectSaveDataType type;

        // データシリアライザで利用するキ－
        private string key;

        public SaveLoadSystem(ProjectSaveDataType type, SaveSystem saveSystem)
        {
            this.type = type;
            this.key = type.ToString();

            SubjectSaveSystemEvent(saveSystem);

            void SubjectSaveSystemEvent(SaveSystem saveSystem)
            {
                saveSystem.SaveEvent += this.Save;
                saveSystem.LoadEvent += this.Load;
                saveSystem.DeleteEvent += this.OnDelete;
                saveSystem.ProjectChangedEvent += this.OnProjectChanged;
            }
        }

        private Action<IProjectID, List<_T>> loadCallback;
        event Action<IProjectID, List<_T>> ISaveLoadSystem<_T>.LoadCallback
        {
            add
            {
                loadCallback += value;
            }

            remove
            {
                loadCallback -= value;
            }
        }

        private Action<IProjectID, List<_T>> deleteCallback;
        event Action<IProjectID, List<_T>> ISaveLoadSystem<_T>.DeleteCallback
        {
            add
            {
                deleteCallback += value;
            }

            remove
            { 
                deleteCallback -= value;
            }
        }

        private Action<IProjectID, List<_T>, List<_T>> projectChangeCallback;
        event Action<IProjectID, List<_T>, List<_T>> ISaveLoadSystem<_T>.ProjectChangeCallback
        {
            add
            {
                projectChangeCallback += value;
            }

            remove
            {
                projectChangeCallback -= value;
            }
        }

        bool ISaveLoadSystem<_T>.TryAddData(_T data)
        {
            if (saveDatas.Any(x => x.ID == data.ID))
            {
                return false;
            }
            saveDatas.Add(data);
            ProjectSaveDataManager.Add(
                type,
                data.ID);
            return true;
        }

        bool ISaveLoadSystem<_T>.TryRemoveData(string id)
        {
            var data = saveDatas.FirstOrDefault(x => x.ID == id);
            if (data == null)
            {
                return false;
            }
            saveDatas.Remove(data);

            ProjectSaveDataManager.Delete(
                type,
                data.ID);
            return true;
        }

        void ISaveLoadSystem<_T>.Update(_T data)
        {
            var id = saveDatas.FindIndex(saveDatas => saveDatas.ID == data.ID);
            saveDatas[id] = data;
        }

        private void Load(string projectID)
        {
            var datas = DataSerializer.Load<List<_T>>(key);
            //if (datas == null || datas.Count == 0)
            //{
            //    return;
            //}

            if (datas == null)
                datas = new List<_T>();
            loadCallback?.Invoke(new ProjectID(projectID), datas);
        }

        private void Save(string projectID)
        {
            List<_T> filteredData;
            if (string.IsNullOrEmpty(projectID))
            {
                filteredData = saveDatas;
            }
            else
            {
                filteredData = saveDatas
                    .Where(data => ProjectSaveDataManager.TryCheckData(
                        type,
                        projectID,
                        data.ID))
                    .ToList();
            }

            DataSerializer.Save(key, filteredData);
        }

        private void OnDelete(string projectID)
        {
            var deleteList = saveDatas.Where(data => ProjectSaveDataManager.TryCheckData(
                    type,
                    projectID,
                    data.ID,
                    false))
                .ToList();

            foreach (var data in deleteList)
            {
                saveDatas.Remove(data);
            }
            deleteCallback?.Invoke(new ProjectID(projectID), deleteList);
        }

        private void OnProjectChanged(string projectID)
        {
            var canEditList = saveDatas.Where(data => ProjectSaveDataManager.TryCheckData(
                    type,
                    projectID,
                    data.ID))
                .ToList();
            var notEditList = saveDatas.Except(canEditList).ToList();

            projectChangeCallback?.Invoke(new ProjectID(projectID), canEditList, notEditList);
        }


    }
}

