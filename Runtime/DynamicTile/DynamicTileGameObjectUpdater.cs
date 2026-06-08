using PLATEAU.CityInfo;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.DynamicTile
{
    /// <summary>
    /// 動的タイル用ゲームオブジェクトラッパークラスを更新するクラス
    /// </summary>
    public class DynamicTileGameObjectUpdater : ISubComponent
    {
        private static DynamicTileGameObjectUpdater instance;

        /// <summary>
        /// インスタンス化
        /// シングルトン
        /// </summary>
        /// <returns></returns>
        public static void Instantiate()
        {
            if (instance != null)
            {
                Debug.LogError("シングルトンのインスタンスを複数生成しようとした");
            }
            instance = new DynamicTileGameObjectUpdater();
        }

        public static ISubComponent GetSubComponet()
        {
            return instance;
        }

        // DynamicTileGameObjectUpdaterが有効か
        public static bool IsEnable { get => instance != null; }

        // 管理中のDynamicTileGameObject
        // CreateOrGet()で必要な分だけ追加される
        // todo:現状減ることが無いのでどこも参照を持っていないなら解放する処理があると嬉しい
        readonly Dictionary<string, DynamicTileGameObject> gameObjects;

        private DynamicTileGameObjectUpdater()
        {
            gameObjects = new Dictionary<string, DynamicTileGameObject>();
        }

        /// <summary>
        /// 動的タイルの更新イベントへの登録
        /// </summary>
        /// <param name="rootGroupFilter"></param>
        public static void SubjectDynamicTileEvent(INotifyUpdated notifyUpdated)
        {
            Debug.Assert(instance != null, "DynamicTileGameObjectUpdaterがInstantiateされていません");
            var rootGroupFilter = notifyUpdated.FindFromInstantiatedFileter<RootGroupFilter>();
            rootGroupFilter.EvUpdated += instance.Update;
        }

        /// <summary>
        /// DynamicTileGameObjectを生成、既存のものがあれば取得
        /// 生成時には管理コレクションに追加
        /// こちらのCreateOrGet()で生成するとDynamicTileGameObject内のCityObjectは使用できない
        /// memo:Release()も必要になったら実装する
        /// </summary>
        /// <param name="newGameObject"></param>
        /// <returns></returns>
        public static DynamicTileGameObject CreateOrGet(GameObject newGameObject)
        {
            Debug.Assert(instance != null, "DynamicTileGameObjectUpdaterがInstantiateされていません");
            // todo:PLATEAUCityObjectGroupからズームレベルの違うオブジェクトを取得できるといい
            var newID = newGameObject.name;
            if (instance.gameObjects.TryGetValue(newID, out var dObj))
                return dObj;

            dObj = new DynamicTileGameObject(newGameObject);
            instance.gameObjects.Add(newID, dObj);
            return dObj;
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="group"></param>
        private void Update(GameObject go)
        {
            var id = go.name;
            if (!gameObjects.TryGetValue(id, out var dObj))
                return;

            dObj.Update(go);
        }

        public void Update(float deltaTime)
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        public void Start()
        {
        }


    }
}
