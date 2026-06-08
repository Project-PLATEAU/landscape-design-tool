using PlateauToolkit.Sandbox;
using PlateauToolkit.Sandbox.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.Common
{
    /// <summary>
    /// 景観ツールで配置可能なオブジェクトに関するユーティリティクラス
    /// </summary>
    public static class LandscapeToolAssetUtil
    {
        /// <summary>
        /// LODGroupを無効化し最も高いLODのみ表示するようにする
        /// (メッシュやバウンディングボックスを取得している他の箇所でも最も高いLODが利用されていることが多いので合わせる)
        /// memo 配置したアセットがLODによってカリングされないのを意図しています(cameraのfarclip以外でカリングされないのを想定しています)
        /// </summary>
        /// <param name="obj"></param>
        public static void DisableLodGroup(GameObject obj)
        {
            // LODGroupが付いているAssetは全てdisableにする
            if (obj.TryGetComponent<LODGroup>(out var lodGroup))
            {
                lodGroup.enabled = false;

                // 最も高いLOD以外のRendererを無効化
                var lods = lodGroup.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (var renderer in lods[i].renderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = false;
                        }
                    }
                }
            }
        }

    }

    public static class AdvertisementObjectUtil
    {
        /// <summary>
        /// 親階層含めて再帰的に調べて、指定したコンポーネントを持つGameObjectを取得する
        /// </summary>
        /// <typeparam name="T">調べたいコンポーネント型</typeparam>
        /// <param name="obj">開始するGameObject</param>
        /// <param name="maxDepth">再帰する最大回数（親の深さ）</param>
        /// <returns>見つかったGameObject。なければnull</returns>
        public static T FindWithComponent<T>(GameObject obj, int maxDepth) where T : Component
        {
            Transform current = obj.transform;
            int depth = 0;
            while (current != null && depth < maxDepth)
            {
                if (current.TryGetComponent<T>(out T comp))
                {
                    return comp;
                }
                current = current.parent;
                depth++;
            }
            return null;
        }

        /// <summary>
        /// 名前でフィルターされた対象の広告物かどうかを判定する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nameFilter"></param>
        /// <returns></returns>
        public static bool IsTargetAdObjWithNameFilter(GameObject obj, List<string> nameFilter)
        {
            if (obj == null || nameFilter == null)
            {
                return false;
            }

            if (nameFilter.Count == 0)
            {
                return true; // フィルターが空なら全てのオブジェクトが対象
            }

            // 名前フィルターに一致するかどうかをチェック
            foreach (var filter in nameFilter)
            {
                if (obj.name.StartsWith(filter))
                {
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// 対象の広告物かどうかを判定する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsTargetAdObj(GameObject obj)
        {
            var isTarget = FindAdObjRoot(obj);
            return isTarget;
        }

        public static GameObject FindAdObjRoot(GameObject obj)
        {

            if (obj.TryGetComponent<PlateauSandboxAdvertisement>(out var _))
            {
                return obj;
            }
            else
            {
                const int MaxComponentSearchDepth = 10; // depthはてきとう
                var comp = FindWithComponent<PlateauSandboxAdvertisementScaled>(obj, MaxComponentSearchDepth); 
                if (comp != null)
                {
                    return comp.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 配置されている広告物オブジェクトを取得する
        /// 広告物の種類の判別は配置直後の名前で行っている
        /// </summary>
        /// <param name="nameFilter"></param>
        /// <returns></returns>
        public static List<PlateauSandboxPlaceableHandler> GetPlateauSandboxAdvertisements(List<string> nameFilter = null)
        {
            // 広告物オブジェクトの取得
            var adObjs = GameObject.FindObjectsByType<PlateauSandboxAdvertisement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var targetAdObjs = new System.Collections.Generic.List<PlateauSandboxPlaceableHandler>(adObjs.Length);
            foreach (var item in adObjs)
            {
                if (nameFilter != null && nameFilter?.Count > 0)
                {
                    if (IsTargetAdObjWithNameFilter(item.gameObject, nameFilter))
                        targetAdObjs.Add(item);
                }
                else
                {
                    targetAdObjs.Add(item);
                }
            }

            var scalableAdObjs = GameObject.FindObjectsByType<PlateauSandboxAdvertisementScaled>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var targetScalableAdObjs = new System.Collections.Generic.List<PlateauSandboxPlaceableHandler>(scalableAdObjs.Length);
            foreach (var item in scalableAdObjs)
            {
                if (nameFilter != null && nameFilter?.Count > 0)
                {
                    if (IsTargetAdObjWithNameFilter(item.gameObject, nameFilter))
                        targetScalableAdObjs.Add(item);
                }
                else
                {
                    targetScalableAdObjs.Add(item);
                }
            }

            targetAdObjs.AddRange(targetScalableAdObjs);
            return targetAdObjs;
        }

        public static bool GetBoundsFromPlateauSandboxAdvertisement(PlateauSandboxPlaceableHandler plateauSandboxAdvertisement, out Bounds bounds)
        {
            bounds = new Bounds();

            if (plateauSandboxAdvertisement == null)
            {
                Debug.LogWarning("PlateauSandboxAdvertisement is null.");
                return false;
            }

            if (plateauSandboxAdvertisement is PlateauSandboxAdvertisement)
            {
                if (GetBoundsFromPlateauSandboxAdvertisement(plateauSandboxAdvertisement, out bounds)) // local function
                {
                    return true;
                }
            }
            else if (plateauSandboxAdvertisement is PlateauSandboxAdvertisementScaled scaled)
            {
                if (GenerateFromPlateauSandboxAdvertisementScaled(scaled, out bounds))
                {
                    return true;
                }
            }

            return false;

            static bool GetBoundsFromMeshFilter(GameObject adObject, out Bounds bounds)
            {
                bounds = new Bounds();

                var meshFilter = adObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    return false;
                }
                if (meshFilter.sharedMesh == null)
                {
                    return false;
                }

                bounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            static bool GetBoundsFromPlateauSandboxAdvertisement(PlateauSandboxPlaceableHandler plateauSandboxAdvertisement, out Bounds bounds)
            {
                // MeshFilterが存在する場合はそのまま取得
                if (GetBoundsFromMeshFilter(plateauSandboxAdvertisement.gameObject, out bounds))
                {
                    return true;
                }

                // 無い場合はLODGroupから取得を試みる
                // MeshFilter、Rendererコンポーネントは実際に描画行っているオブジェクトから取得する必要がある。LODGroupで最も高解像度のものを利用する
                var lodGroup = plateauSandboxAdvertisement.GetComponent<LODGroup>();
                if (lodGroup)
                {
                    var lod = lodGroup.GetLODs()[0];   // 最も高いLODを取得
                    if ((lod.renderers.Length == 1) == false)
                    {
                        Debug.LogError("No renderers found in the LODGroup. Cannot get bounds.");
                        return false;
                    }
                    if (GetBoundsFromMeshFilter(lod.renderers[0].gameObject, out bounds) == false)
                    {
                        return false;
                    }
                    return true;
                }

                // LODGroupが無い場合は、どこかにある MeshFileter を持つゲームオブジェクトを検索
                // 子を走査する
                var meshFileter = plateauSandboxAdvertisement.gameObject.GetComponentInChildren<MeshFilter>();
                var current = plateauSandboxAdvertisement.gameObject;

                Bounds bbounds = default;
                var r = Traverse(current.transform, 10, (child) =>
                {
                    // MeshFilterが存在する場合はそのまま取得
                    if (GetBoundsFromMeshFilter(child.gameObject, out bbounds))
                    {
                        return true;
                    }
                    return false;
                });

                if (r ==false)
                {
                    return false;
                }

                bounds = bbounds;

                return true;
            }



            static bool GenerateFromPlateauSandboxAdvertisementScaled(PlateauSandboxAdvertisementScaled scaled, out Bounds bounds)
            {
                Vector3 center = scaled.transform.GetChild(0).transform.localPosition;   // Rootオブジェクトを基準にする
                Vector3 size = new Vector3(
                    scaled.BillboardSize.x,
                    (scaled.BillboardSize.y + scaled.PoleHeight),
                    scaled.BillboardSize.z);

                bounds = new Bounds(center, size); // Boundsは中心とサイズを指定して生成 ローカル座標系

                return true;
            }

        }

        /// <summary>
        /// 指定した maxDepth まで子を再帰的に走査し、各ノードで onVisit を呼び出します。
        /// maxDepth: 親からの階層数（1=直下の子のみ、2=孫まで）
        /// </summary>
        public static bool Traverse(this Transform root, int maxDepth, Func<Transform, bool> onVisit)
        {
            if (root == null || maxDepth < 1)
                return false;

            return TraverseInternal(root, 1, maxDepth, onVisit);
        }

        static bool TraverseInternal(Transform node, int currentDepth, int maxDepth, Func<Transform, bool> onVisit)
        {
            foreach (Transform child in node)
            {
                if (onVisit?.Invoke(child) ?? true)
                    return true; 

                if (currentDepth < maxDepth)
                {
                    if (TraverseInternal(child, currentDepth + 1, maxDepth, onVisit))
                        return true; // 子の走査で onVisit が true を返した場合
                }
            }

            return false; // 全ての子を走査しても onVisit が true を返さなかった場合
        }

        public static List<Vector3> CalcBoundBottomCorners(Bounds bounds, Transform transform)
        {
            ////var collider = adObject.GetComponent<Collider>();
            ////var bounds = collider.bounds;
            ////DebugBoundsDrawer.DrawBoundingBox(bounds, Color.green, 20f, false);
            ////bounds.Encapsulate


            // Bounds の中心と Extents を取得
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;


            // 下側4頂点を格納する配列
            const int numCorner = 4;
            List<Vector3> bottomCorners = new List<Vector3>(numCorner);

            //Vector3 CalcVectorOffset(Quaternion rotate, Vector3 offset) => rotate * offset;
            //Vector3 CalcOffset(Quaternion rotate, float x, float y, float z) => CalcVectorOffset(rotate, new Vector3(x, y, z));

            // 頂点を計算（Y はすべて center.y - extents.y = bounds.min.y）右回り
            var rot = transform.rotation;

            bottomCorners.Add(center + new Vector3(-extents.x, -extents.y, -extents.z));  // 左前
            bottomCorners.Add(center + new Vector3(-extents.x, -extents.y, extents.z));  // 左奥
            bottomCorners.Add(center + new Vector3(extents.x, -extents.y, extents.z));  // 右奥
            bottomCorners.Add(center + new Vector3(extents.x, -extents.y, -extents.z));  // 右前

            // ワールド座標に変換 Mesh.boundsだと必要
            for (int i = 0; i < numCorner; i++)
            {
                bottomCorners[i] = transform.TransformPoint(bottomCorners[i]);
            }

            // デバッグ表示
            for (int i = 0; i < numCorner; i++)
            {
                Debug.DrawRay(bottomCorners[i], Vector3.up * 10f, Color.red, 10f);
            }

            return bottomCorners;
        }

    }
}
