using Landscape2.Runtime.DynamicTile;
using PLATEAU.CityGML;
using PLATEAU.CityInfo;
using System.Linq;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    public static class LandscapeRaycast
    {
        /// <summary>
        /// 非推奨
        /// 他のRaycast使用箇所を確認して整備が必要
        /// 
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hitInfo"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, int layerMask = -5)
        {
            hitInfo = new RaycastHit();

            var distance = float.MaxValue;
            var results = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue, layerMask:layerMask);
            if (results.Length == 0)
            {
                return false;
            }
            foreach (var result in results)
            {
                var go = result.collider.gameObject;

                if (CityObjectUtil.IsSelectable(go) == false)
                {
                    continue;
                }

                if (result.distance >= distance)
                {
                    continue;
                }

                distance = result.distance;
                hitInfo = result;
            }

            return distance != float.MaxValue;
        }

        public static bool RaycastToBuilding(Ray ray, out RaycastHit hitInfo, int layerMask)
        {
            hitInfo = new RaycastHit();

            var distance = float.MaxValue;
            var results = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue, layerMask:layerMask);
            if (results.Length == 0)
            {
                return false;
            }
            foreach (var result in results)
            {
                var go = result.collider.gameObject;

                // 選択可能な建物かどうか
                if (CityObjectUtil.IsSelectableBuilding(go) == false)
                {
                    continue;
                }

                if (result.distance >= distance)
                {
                    continue;
                }

                distance = result.distance;
                hitInfo = result;
            }

            return distance != float.MaxValue;
        }

        //public static bool RaycastToPLATEAUCityObjectGroup(Ray ray, out RaycastHit hitInfo)
        //{
        //    hitInfo = new RaycastHit();

        //    var distance = float.MaxValue;
        //    var results = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue);
        //    if (results.Length == 0)
        //    {
        //        return false;
        //    }
        //    foreach (var result in results)
        //    {
        //        var go = result.collider.gameObject;

        //        if (CityObjectUtil.IsSelectable(go) == false)
        //        {
        //            continue;
        //        }

        //        //if (go.TryGetComponent<PLATEAUCityObjectGroup>(out _) == false)
        //        //{
        //        //    continue;
        //        //}

        //        if (result.distance >= distance)
        //        {
        //            continue;
        //        }

        //        distance = result.distance;
        //        hitInfo = result;
        //    }

        //    return distance != float.MaxValue;
        //}


    }

    /// <summary>
    /// PLATEAUのCityObjectを扱うためのユーティリティクラス
    /// </summary>
    public static class CityObjectUtil
    {
        public static bool IsBuilding(GameObject cityObject)
        {
            if (cityObject.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                // 建物かどうかの判定
                if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == CityObjectType.COT_Building))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static bool IsGround(GameObject cityObject)
        {
            // memo 動的タイルだと属性情報がほぼ空っぽだったのでこの関数内のSMOOTHED_groupで判定を取る
            if (cityObject.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                // 地面かどうかの判定
                if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == CityObjectType.COT_TINRelief))
                {
                    return true;
                }
            }

            // ToDo 次の命名規則になることを共通する箇所から取得出来るようにしたい。もしくは他の方法で"TERRAIN_dem"を識別できるようにしたい
            // ConvertedTerrainData PlaceToSceneRecursive()内でこうなるように名前が付けられる
            var terrainDemPrefix = "TERRAIN_dem";
            if (cityObject.name.Contains(terrainDemPrefix))  //if the ground object is found
            {
                return true;
            }

            // ToDo 次の命名規則になることを共通する箇所から取得出来るようにしたい。もしくは他の方法で"SMOOTHED_dem"を識別できるようにしたい
            // 通常インポート専用の判定処理
            // 景観ツール向けに設定を行うと上記のPLATEAUCityObjectGroupから建物と同じように判定が取れなくなるため、プレフィックスで判定する。
            // 通常インポート→高さ合わせ→アセット化→景観ツール初期化実行
            var tileDemPrefix = "SMOOTHED_dem";
            if (cityObject.name.Contains(tileDemPrefix))  //if the ground object is found
            {
                return true;
            }

            // ToDo 次の命名規則になることを共通する箇所から取得出来るようにしたい。もしくは他の方法で"SMOOTHED_group"を識別できるようにしたい
            // 動的タイル専用の判定処理
            // 景観ツール向けに設定を行うと上記のPLATEAUCityObjectGroupから建物と同じように判定が取れなくなるため、プレフィックスで判定する。
            // 動的タイルインポート→分割機能→高さ合わせ→景観ツール初期化実行
            var dynamicTileDemPrefix = "SMOOTHED_group";
            if (cityObject.name.Contains(dynamicTileDemPrefix))  //if the ground object is found
            {
                return true;
            }

            return false;
        }

        public static bool IsTran(GameObject cityObject)
        {
            if (cityObject.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                return IsTran(cityObjectGroup);
            }

            // ToDo 名前以外から判定出来るようにしたい。
            // 動的タイル専用の判定処理。PLATEAUCityObjectGroupがアタッチされない。
            // 動的タイルインポート→分割機能→高さ合わせ→景観ツール初期化実行
            var dynamicTileTranPrefix = "ALIGNED_tran";
            if (cityObject.name.Contains(dynamicTileTranPrefix))  //if the ground object is found
            {
                return true;
            }

            return false;
        }

        private static bool IsTran(PLATEAUCityObjectGroup cityObjectGroup)
        {
            if (cityObjectGroup)
            {
                // 地面かどうかの判定
                if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == CityObjectType.COT_Road))
                {
                    return true;
                }

            }
            return false;
        }

        public static string GetGmlID(DynamicTileGameObject targetObj)
        {
            // GmlIDは参照に依存しないため CurrentGameObjectを利用しても大丈夫なはず
            return GetGmlID(targetObj.CurrentGameObject);
        }

        public static string GetGmlID(GameObject targetObj)
        {
            if (targetObj.TryGetComponent<PLATEAUCityObjectGroup>(out var cityObjectGroup))
            {
                foreach (var cityObject in cityObjectGroup.GetAllCityObjects())
                {
                    return cityObject.GmlID;
                }
            }
            return "";
        }

        public static bool IsSelectable(GameObject go)
        {
            // これを入れると広告物などのアセットが選択出来なくなるためコメントアウト
            //if (go.TryGetComponent<PLATEAUCityObjectGroup>(out _) == false)
            //{
            //    return false;
            //}

            // すべてにすると広告物などのアセットが選択出来なくなるため制限
            if (IsBuilding(go))
            {
                if (go.TryGetComponent<Renderer>(out var renderer))
                {
                    // 非表示のオブジェクトはスルー
                    return renderer.enabled;
                }
                else
                {
                    return false;
                }

            }

            return true;
        }

        /// <summary>
        /// 選択可能な建物かどうか
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static bool IsSelectableBuilding(GameObject go)
        {
            if (IsSelectable(go) == false)
            {
                return false;
            }

            if (!CityObjectUtil.IsBuilding(go))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 都市モデルのバウンディングボックスを計算する
        /// ただし、使用している箇所の都合で建物と地面のみ対象だったり、地面のみ対象なったりする
        /// 仕様が決まったら関数名も変更予定
        /// </summary>
        /// <param name="cityObject"></param>
        /// <returns></returns>
        public static Bounds CalcCityModelBounds(GameObject cityObject)
        {
            var bounds = new Bounds();
            var groups = GameObject.FindObjectsByType<PLATEAUCityObjectGroup>(FindObjectsSortMode.None);
            foreach (var group in groups)
            {
                // 建物と地面のみ対象
                if (/*!IsBuilding(group.gameObject) && */!IsGround(group.gameObject))
                    continue;

                if (group.gameObject.TryGetComponent<Collider>(out var col))
                {
                    bounds.Encapsulate(col.bounds);
                }
            }

            // boundsをdrawLineで描画する
            DrawBounds(bounds, Color.red, 180f);
            void DrawBounds(Bounds bounds, Color color, float duration = 0f)
            {
                // 8 corners of the bounds
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;

                Vector3 p0 = new Vector3(min.x, min.y, min.z);
                Vector3 p1 = new Vector3(max.x, min.y, min.z);
                Vector3 p2 = new Vector3(max.x, min.y, max.z);
                Vector3 p3 = new Vector3(min.x, min.y, max.z);

                Vector3 p4 = new Vector3(min.x, max.y, min.z);
                Vector3 p5 = new Vector3(max.x, max.y, min.z);
                Vector3 p6 = new Vector3(max.x, max.y, max.z);
                Vector3 p7 = new Vector3(min.x, max.y, max.z);

                // Bottom
                Debug.DrawLine(p0, p1, color, duration);
                Debug.DrawLine(p1, p2, color, duration);
                Debug.DrawLine(p2, p3, color, duration);
                Debug.DrawLine(p3, p0, color, duration);

                // Top
                Debug.DrawLine(p4, p5, color, duration);
                Debug.DrawLine(p5, p6, color, duration);
                Debug.DrawLine(p6, p7, color, duration);
                Debug.DrawLine(p7, p4, color, duration);

                // Sides
                Debug.DrawLine(p0, p4, color, duration);
                Debug.DrawLine(p1, p5, color, duration);
                Debug.DrawLine(p2, p6, color, duration);
                Debug.DrawLine(p3, p7, color, duration);
            }
            return bounds;
        }

    }
}