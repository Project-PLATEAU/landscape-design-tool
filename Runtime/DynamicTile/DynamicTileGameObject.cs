using PLATEAU.CityInfo;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.DynamicTile
{
    /// <summary>
    /// 動的タイル環境用のGameObject
    /// 通常のGameObjectと似たような感じで扱えるようにするラッパー
    /// クラスの拡張時にはズームレベルの更新によって新規生成されるGameObjectに対して同様の処理を行うためにデータ変更の追跡は考慮する必要がある
    /// </summary> 
    public class DynamicTileGameObject
    {
        /// <summary>
        /// 動的タイルによる更新によって生成されたオブジェクトで継承したい処理を提供する
        /// </summary>
        public interface IInherit
        {
            /// <summary>
            /// 生成直後に行われる
            /// 利用例
            /// Rendererの表示状態を持ち越したい
            /// </summary>
            void Init(GameObject newGameObject);

        }

        // PLATEAUCityObjectGroupコンポーネントがアタッチされたゲームオブジェクトの名前
        // ToDo:現状キーとして利用してしまっている。PLATEAUCityObjectGroupコンポーネントから同一オブジェクトか判定できると嬉しい
        public string name { get; private set; }
        public int layer { get; private set; }

        // 動的タイルの更新によって常に有効とは限らないので注意 あまり使わない方が保守性が上がるはず
        public GameObject CurrentGameObject { get => current; }
        GameObject current;

        // GameOjectに行う処理　内部のGameObjectが更新された後、復元のために実行する
        readonly System.Collections.Generic.Dictionary<string, IInherit> initCommands;

        /// <summary>
        /// 使用禁止
        /// このクラスのインスタンスか、内部のGameObjectのどちらと比較しているか分かりづらいため
        /// </summary>
        [System.Obsolete("このメソッドは非推奨です。")]
        public static bool operator ==(DynamicTileGameObject left, object right)
        {
            return object.ReferenceEquals(left.current, right);
        }

        /// <summary>
        /// 使用禁止
        /// このクラスのインスタンスか、内部のGameObjectのどちらと比較しているか分かりづらいため
        /// </summary>
        [System.Obsolete("このメソッドは非推奨です。")]
        public static bool operator !=(DynamicTileGameObject left, object right)
        {
            return !object.ReferenceEquals(left.current, right);
        }

        /// <summary>
        /// 使用禁止
        /// このクラスのインスタンスか、内部のGameObjectのどちらと比較しているか分かりづらいため
        /// </summary>
        [System.Obsolete("このメソッドは非推奨です。")]
        public override bool Equals(object obj)
        {
            return object.Equals(this.current, obj);
        }

        /// <summary>
        /// ハッシュ取得
        /// 内部のGameObjectは更新されるのでハッシュ値が変化してしまうためこのクラスから参照
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// こっちで初期化した場合 cityObjectは使用できない。
        /// todo : 分かりやすくする
        /// </summary>
        /// <param name="newGameObject"></param>
        public DynamicTileGameObject(GameObject newGameObject)
        {
            Debug.Assert(DynamicTileGameObjectUpdater.IsEnable);
            initCommands = new Dictionary<string, IInherit>();
            Init(newGameObject);

        }

        /// <summary>
        /// 更新処理
        /// Updater外では呼び出すことを考慮していない
        /// todo:interfaceでメソッドの役割の分担を明確にする
        /// </summary>
        /// <param name="newGameObject"></param>
        /// <returns></returns>
        public bool Update(GameObject newGameObject)
        {
            if (string.Compare(name, newGameObject.name) != 0)
                return false;

            Init(newGameObject);
            foreach (var item in initCommands)
            {
                item.Value.Init(newGameObject);
            }
            return true;
        }

        private void Init(GameObject newGameObject)
        {
            this.current = newGameObject;
            this.name = this.current.name;
            this.layer = this.current.layer;
        }

        /// <summary>
        /// TryGetComponentのラップ
        /// 注意　副作用がある処理を行うのは禁止　値の取得のみ許可する
        /// todo:取得したコンポーネントに操作を行うと追跡出来ないのでそれの対策したい 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public bool TryGetRawComponent<T>(out T component) where T : Component
        {
            if (current != null)
                return current.TryGetComponent<T>(out component);
            component = default;
            return false;
        }

        /// <summary>
        /// GetComponentのラップ
        /// 注意　副作用がある処理を行うのは禁止　値の取得のみ許可する
        /// todo:取得したコンポーネントに操作を行うと追跡出来ないのでそれの対策したい 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRawComponent<T>() where T : Component
        {
            if (current != null)
                return current.GetComponent<T>();
            return default;
        }

        // todo:取得したコンポーネントに操作を行うと追跡出来ないのでそれの対策
        /// <summary>
        /// AddComponentのラップ
        /// 注意　ここで追加したコンポーネントはズームレベル更新後の新規GameObjectでは消えてしまう
        /// GetOrAddInit()と組み合わせて状態を継承するように対応する必要がある
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T AddRawComponent<T>() where T : Component
        {
            if (current != null)
                return current.AddComponent<T>();
            return null;
        }

        /// <summary>
        /// 状態の継承処理の取得、追加
        /// 動的タイル更新後の新規GameObjectに継承したい設定を引き継ぐ為に使う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <returns>false:取得 true:追加</returns>
        public bool GetOrAddInit<T>(string key, out T action)
            where T : class, IInherit, new()
        {
            bool isNewlyAdded = !initCommands.ContainsKey(key);
            if (isNewlyAdded)
            {
                initCommands.Add(key, new T());
            }

            action = initCommands[key] as T;
            return isNewlyAdded;
        }

        /// <summary>
        /// ズームレベルが異なるだけで同じ都市モデルか確認する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsSame(DynamicTileGameObject obj)
        {
            return string.Compare(name, obj.name) == 0;
        }

        /// <summary>
        /// 内部のインスタンスを保持しているか
        /// 引数がnullの場合は内部もnullとして扱う
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool HasInstance(DynamicTileGameObject obj)
        {
            if (obj as object == null)  // DynamicTileGameObjectのインスタンスと比較
                return false;
            return obj.current != null;
        }
    }

}
