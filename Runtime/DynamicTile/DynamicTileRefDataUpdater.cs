// 動的タイルマネージャのイベントを購読する イベントが存在しないなどエラーが出た場合SDK側のバージョンを上げてください。
#define SUBSCRIBE_TILE_MANAGER_EV

using Landscape2.Runtime.Common;
using PLATEAU.CityInfo;
using PLATEAU.DynamicTile;
using PLATEAU.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Landscape2.Runtime.DynamicTile
{


    /// <summary>
    /// データ更新用のインターフェース
    /// ここにデータ更新のメソッドを登録する
    /// </summary>
    public interface INotifyUpdated
    {
        /// <summary>
        /// タイルがインスタンス化される時に呼ばれるコレクションから検索する
        /// </summary>
        _T FindFromInstantiatedFileter<_T>()
            where _T : class, IDataFilter;

        /// <summary>
        /// タイルが破棄される時に呼ばれるコレクションから検索する
        /// </summary>
        _T FindFromBeforeUnloadFileter<_T>()
            where _T : class, IDataFilter;

    }

    /// <summary>
    /// 更新機能
    /// 動的タイルの更新イベントなどに合わせて呼び出す
    /// </summary>
    public interface IDynamicTileUpdater
    {
        /// <summary>
        /// タイルがインスタンス化された
        /// </summary>
        void OnTileInstantiated(GameObject o);
        /// <summary>
        /// タイルが破棄される
        /// </summary>
        void OnBeforeTileUnload(GameObject o);

    }

    /// <summary>
    /// 動的タイルによる更新を監視して、参照データの更新を行うクラス
    /// </summary>
    public class DynamicTileRefDataUpdater : ISubComponent, IDynamicTileUpdater, INotifyUpdated
    {
        private readonly DynamicTile.FilterCollection instantiatedFilters = new FilterCollection();
        private readonly DynamicTile.FilterCollection beforeUnloadFilters = new FilterCollection();

        bool isNeedManualnit = true;

        public DynamicTileRefDataUpdater()
        {
            // TileMangerの有無を問わずフィルターは用意する。初期化に利用するため
            instantiatedFilters.Build();
            beforeUnloadFilters.Build();

            var tMana = GameObject.FindFirstObjectByType<PLATEAUTileManager>();
            if (tMana == null)
                return;

            isNeedManualnit = false;

#if SUBSCRIBE_TILE_MANAGER_EV
            var p = this as IDynamicTileUpdater;
            tMana.onTileInstantiated.AddListener(p.OnTileInstantiated);
            tMana.beforeTileUnload.AddListener(p.OnBeforeTileUnload);
#endif

        }

        _T INotifyUpdated.FindFromInstantiatedFileter<_T>()
        {
            return instantiatedFilters.Find<_T>();
        }

        _T INotifyUpdated.FindFromBeforeUnloadFileter<_T>()
        {
            return beforeUnloadFilters.Find<_T>();
        }

        void ISubComponent.Update(float deltaTime)
        {
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {
            if (isNeedManualnit == true)
            {
                var groupComponents = GameObject.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
                var childGameObjects = Array.ConvertAll(groupComponents, g => g.gameObject);
                UpdateFileter(childGameObjects, instantiatedFilters);
                isNeedManualnit = false;
            }
        }

        void ISubComponent.OnEnable()
        {
        }

        void ISubComponent.OnDisable()
        {
        }

        void ISubComponent.Start()
        {

        }

        void IDynamicTileUpdater.OnTileInstantiated(GameObject o)
        {
            var groupComponents = o.GetComponentsInChildren<MeshFilter>();
            var childGameObjects = Array.ConvertAll(groupComponents, g => g.gameObject);
            UpdateFileter(childGameObjects, instantiatedFilters);
        }

        void IDynamicTileUpdater.OnBeforeTileUnload(GameObject o)
        {
            var groupComponents = o.GetComponentsInChildren<MeshFilter>();
            var childGameObjects = Array.ConvertAll(groupComponents, g => g.gameObject);
            UpdateFileter(childGameObjects, beforeUnloadFilters);
        }

        private static void UpdateFileter(GameObject[] childGameObjects, FilterCollection fileters)
        {
            fileters.rootFilter.UpdateRoot(childGameObjects);
            var i = fileters.rootFilter as IDataFilter;
            Debug.Assert(i != null);
            i.UpdateChildren();
        }

    }
}
