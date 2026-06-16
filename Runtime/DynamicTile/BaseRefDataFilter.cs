using Landscape2.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime.DynamicTile
{
    /// <summary>
    /// データをフィルタリング、抽出するクラスのインターフェイス
    /// この機能は親子関係を組める
    /// 例えば　
    /// 都市モデル群から建物だけ抽出する機能、対象のゲームオブジェクトからバウンディングボックスやgml_idを取得する機能がある時、
    /// 二つの機能を組み合わせて建物モデル群からバウンディングボックスやgml_idを取得するような機能を生成出来る
    /// </summary>
    public interface IDataFilter
    {
        // 子フィルターの追加
        void Add(IDataFilter child);
        // 子フィルターの更新
        void UpdateChildren();

        // 自身の更新
        void Update(IDataFilter parent);

        // 子のFilterの取得
        public IReadOnlyCollection<IDataFilter> Children { get; }

    }

    /// <summary>
    /// データフィルターの基底クラス
    /// </summary>
    public abstract class BaseRefDataFilter : IDataFilter
    {
        private readonly List<IDataFilter> children = new List<IDataFilter>();

        IReadOnlyCollection<IDataFilter> IDataFilter.Children => children.AsReadOnly();

        /// <summary>
        /// データの更新
        /// overrideして使用する
        /// </summary>
        /// <param name="parent"></param>
        protected virtual void UpdateData(IDataFilter parent)
        {
            // 何も実装しない
            throw new NotImplementedException();
        }

        void IDataFilter.Add(IDataFilter child)
        {
            children.Add(child);
        }

        void IDataFilter.UpdateChildren()
        {
            UpdateChildren();
        }

        void IDataFilter.Update(IDataFilter parent)
        {
            // フィルター内のデータを更新
            UpdateData(parent);

            // 子要素の更新
            UpdateChildren();
        }

        private void UpdateChildren()
        {
            foreach (var child in children)
            {
                child.Update(this);
            }
        }

    }

    /// <summary>
    /// すべてのフィルターのルート
    /// </summary>
    public class RootGroupFilter : BaseRefDataFilter
    {
        private GameObject[] originals = null;
        public IReadOnlyCollection<GameObject> Originals => originals;
        public event System.Action<GameObject> EvUpdated;

        /// <summary>
        /// Root専用のUpdateメソッド
        /// </summary>
        public void UpdateRoot(GameObject[] gameObjects)
        {
            originals = gameObjects;
            foreach (var item in originals)
            {
                EvUpdated?.Invoke(item);
            }
        }

        protected override void UpdateData(IDataFilter parent)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 建物のみ取得するフィルター
    /// </summary>
    public class BuildingGroupFileter : BaseRefDataFilter
    {
        private readonly List<GameObject> buildings = new List<GameObject>();
        public IReadOnlyCollection<GameObject> Buildings => buildings;
        public event System.Action<GameObject> EvUpdated;

        protected override void UpdateData(IDataFilter parent)
        {
            buildings.Clear();
            var root = parent as RootGroupFilter;
            Debug.Assert(root != null, "BuildingGroupFileter: 親フィルターがRootGroupFilterではありません。");
            foreach (var go in root.Originals.Where(CityObjectUtil.IsBuilding))
            {
                buildings.Add(go);
            }

            foreach (var item in buildings)
            {
                EvUpdated?.Invoke(item);
            }
        }
    }

    /// <summary>
    /// 道路のみ取得するフィルター
    /// </summary>
    public class TranGroupFilter : BaseRefDataFilter
    {
        private readonly List<GameObject> trans = new List<GameObject>();
        public IReadOnlyCollection<GameObject> Trans => trans;
        public event System.Action<GameObject> EvUpdated;

        protected override void UpdateData(IDataFilter parent)
        {
            trans.Clear();
            var root = parent as RootGroupFilter;
            Debug.Assert(root != null, "TranGroupFilter: 親フィルターがRootGroupFilterではありません。");
            foreach (var go in root.Originals.Where(
                go => {
                    return CityObjectUtil.IsTran(go);
                }))
            {
                trans.Add(go);
            }

            foreach (var item in trans)
            {
                EvUpdated?.Invoke(item);
            }
        }
    }

    /// <summary>
    /// 道路からメッシュコライダーを取得するフィルター
    /// </summary>
    public class TranMeshColliderGroupFilter : BaseRefDataFilter
    {
        private readonly List<MeshCollider> colliders = new List<MeshCollider>();
        public IReadOnlyCollection<MeshCollider> Colliders => colliders;
        public event System.Action<MeshCollider> EvUpdated;

        protected override void UpdateData(IDataFilter parent)
        {
            colliders.Clear();
            var root = parent as TranGroupFilter;
            Debug.Assert(root != null, $"{nameof(TranMeshColliderGroupFilter)}: 親フィルターが{nameof(TranGroupFilter)}ではありません。");
            foreach (var go in root.Trans)
            {
                if (go.TryGetComponent<MeshCollider>(out var collider))
                {
                    colliders.Add(collider);
                }
            }

            foreach (var item in colliders)
            {
                EvUpdated?.Invoke(item);
            }
        }
    }

    /// <summary>
    /// Filter管理クラス
    /// Filter群の構築、Fileterの取得を行う
    /// </summary>
    public class FilterCollection
    {
        public RootGroupFilter rootFilter = new RootGroupFilter();
        private IDataFilter rootInterface => rootFilter;

        public void Build()
        {
            IDataFilter buildingFilter = new BuildingGroupFileter();
            IDataFilter tranFilter = new TranGroupFilter();

            tranFilter.Add(new TranMeshColliderGroupFilter());

            rootInterface.Add(buildingFilter);
            rootInterface.Add(tranFilter);

        }

        public _T Find<_T>()
            where _T : class, IDataFilter
        {
            return _Find<_T>(rootInterface);
        }

        private static _T _Find<_T>(IDataFilter fileter) where _T : class, IDataFilter
        {
            // 自身が目的の型か確認
            if (fileter is _T t)
            {
                return t;
            }

            // 子要素を再帰的に探索
            foreach (var item in fileter.Children)
            {
                var result = _Find<_T>(item);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
